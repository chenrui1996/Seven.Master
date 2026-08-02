using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Application.Messaging;
using Seven.Domain.Common;
using Seven.Domain.Entities.Alarm;
using Seven.Domain.Enums;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Messaging.Outbox;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Security;

namespace Seven.Infrastructure.Services;

/// <summary>
/// 告警服务：解析报警码、写入记录、推送 SignalR
/// </summary>
public class AlarmService : IAlarmService
{
    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _dataScope;
    private readonly IAlarmPushService _push;
    private readonly IOutboxStore _outbox;
    private readonly AlarmOptions _options;

    /// <summary>构造函数</summary>
    public AlarmService(
        SevenDbContext db,
        ICurrentUserService currentUser,
        IDataScopeService dataScope,
        IAlarmPushService push,
        IOutboxStore outbox,
        IOptions<AlarmOptions> options)
    {
        _db = db;
        _currentUser = currentUser;
        _dataScope = dataScope;
        _push = push;
        _outbox = outbox;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<PageGridData<Sys_Alarm>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_Alarms.AsNoTracking().Where(a => !a.IsDeleted);

        if (!string.IsNullOrWhiteSpace(options.Wheres))
        {
            try
            {
                using var doc = JsonDocument.Parse(options.Wheres);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("name", out var nameEl) &&
                            nameEl.GetString() == "status" &&
                            item.TryGetProperty("value", out var valueEl) &&
                            int.TryParse(valueEl.GetString(), out var status))
                        {
                            query = query.Where(a => a.Status == status);
                        }
                    }
                }
            }
            catch
            {
                /* ignore invalid filter json */
            }
        }

        query = query.OrderByDescending(a => a.CreateDate);
        query = await _dataScope.ApplyCreateIdScopeAsync(query, cancellationToken);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetActiveCountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _db.Sys_Alarms.CountAsync(
            a => a.Status == (int)AlarmStatus.Active && !a.IsDeleted,
            cancellationToken);
        return WebResponseContent.Ok(data: new { count });
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> RaiseAsync(RaiseAlarmRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return WebResponseContent.Error("报警码不能为空");

        var codeDef = await ResolveAlarmCodeAsync(request.Code.Trim(), cancellationToken);
        if (codeDef == null)
            return WebResponseContent.Error($"未配置的报警码: {request.Code}");

        var message = FormatMessage(codeDef.Message, request, codeDef);

        var alarm = new Sys_Alarm
        {
            Code = codeDef.Code,
            Message = message,
            Level = codeDef.Level,
            Category = codeDef.Category,
            Source = request.Source,
            DeviceName = request.DeviceName,
            Status = (int)AlarmStatus.Active,
            ExtraData = request.ExtraData,
            CreateDate = DateTime.Now,
            Creator = _currentUser.UserName ?? "system"
        };

        _db.Sys_Alarms.Add(alarm);
        await _db.SaveChangesAsync(cancellationToken);

        var raisedEvent = new AlarmRaisedEvent
        {
            AlarmId = alarm.Alarm_Id,
            Code = alarm.Code,
            Message = alarm.Message,
            Level = alarm.Level,
            Category = alarm.Category,
            Source = alarm.Source,
            DeviceName = alarm.DeviceName,
            OccurredAt = alarm.CreateDate ?? DateTime.Now
        };
        await _outbox.EnqueueAsync(
            nameof(AlarmRaisedEvent),
            JsonSerializer.Serialize(raisedEvent),
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _push.PushNewAlarmAsync(ToDto(alarm));

        return WebResponseContent.Ok("告警已抛出", data: ToDto(alarm));
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> AcknowledgeAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var alarms = await _db.Sys_Alarms.Where(a => ids.Contains(a.Alarm_Id)).ToListAsync(cancellationToken);
        var userName = _currentUser.UserName ?? "unknown";
        var now = DateTime.Now;

        foreach (var alarm in alarms)
        {
            if (alarm.Status != (int)AlarmStatus.Active) continue;
            alarm.Status = (int)AlarmStatus.Acknowledged;
            alarm.AckUserName = userName;
            alarm.AckDate = now;
            alarm.ModifyDate = now;
            alarm.Modifier = userName;
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var alarm in alarms.Where(a => a.Status == (int)AlarmStatus.Acknowledged))
            await _push.PushAlarmUpdatedAsync(ToDto(alarm));

        return WebResponseContent.Ok("已确认");
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> ClearAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var alarms = await _db.Sys_Alarms.Where(a => ids.Contains(a.Alarm_Id)).ToListAsync(cancellationToken);
        var userName = _currentUser.UserName ?? "unknown";
        var now = DateTime.Now;

        foreach (var alarm in alarms)
        {
            alarm.Status = (int)AlarmStatus.Cleared;
            alarm.ModifyDate = now;
            alarm.Modifier = userName;
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var alarm in alarms)
            await _push.PushAlarmUpdatedAsync(ToDto(alarm));

        return WebResponseContent.Ok("已清除");
    }

    /// <inheritdoc />
    public async Task<PageGridData<Sys_AlarmCode>> GetAlarmCodesAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_AlarmCodes.AsNoTracking().OrderBy(c => c.Code);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> SaveAlarmCodeAsync(Sys_AlarmCode entity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entity.Code))
            return WebResponseContent.Error("报警码不能为空");

        entity.Code = entity.Code.Trim();

        if (entity.AlarmCode_Id > 0)
        {
            var existing = await _db.Sys_AlarmCodes.FirstOrDefaultAsync(c => c.AlarmCode_Id == entity.AlarmCode_Id, cancellationToken);
            if (existing == null) return WebResponseContent.Error("记录不存在");
            existing.Code = entity.Code;
            existing.Message = entity.Message;
            existing.Level = entity.Level;
            existing.Category = entity.Category;
            existing.Enable = entity.Enable;
            existing.Remark = entity.Remark;
            existing.ModifyDate = DateTime.Now;
            existing.Modifier = _currentUser.UserName;
        }
        else
        {
            var dup = await _db.Sys_AlarmCodes.AnyAsync(c => c.Code == entity.Code && !c.IsDeleted, cancellationToken);
            if (dup) return WebResponseContent.Error("报警码已存在");

            entity.CreateDate = DateTime.Now;
            entity.Creator = _currentUser.UserName;
            _db.Sys_AlarmCodes.Add(entity);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("保存成功", data: entity);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> DeleteAlarmCodeAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var codes = await _db.Sys_AlarmCodes.Where(c => ids.Contains(c.AlarmCode_Id)).ToListAsync(cancellationToken);
        foreach (var c in codes) c.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    private async Task<Sys_AlarmCode?> ResolveAlarmCodeAsync(string code, CancellationToken ct)
    {
        var fromDb = await _db.Sys_AlarmCodes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code && c.Enable == 1 && !c.IsDeleted, ct);
        if (fromDb != null) return fromDb;

        var fromConfig = _options.Codes.FirstOrDefault(c =>
            string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        if (fromConfig == null) return null;

        return new Sys_AlarmCode
        {
            Code = fromConfig.Code,
            Message = fromConfig.Message,
            Level = fromConfig.Level,
            Category = fromConfig.Category,
            Enable = 1
        };
    }

    private static string FormatMessage(string template, RaiseAlarmRequest request, Sys_AlarmCode codeDef)
    {
        var result = template;

        if (!string.IsNullOrEmpty(request.DeviceName))
            result = result.Replace("{DeviceName}", request.DeviceName, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(request.Source))
            result = result.Replace("{Source}", request.Source, StringComparison.OrdinalIgnoreCase);

        result = result.Replace("{Code}", codeDef.Code, StringComparison.OrdinalIgnoreCase);

        if (request.Params != null)
        {
            foreach (var (key, value) in request.Params)
                result = result.Replace("{" + key + "}", value, StringComparison.OrdinalIgnoreCase);
        }

        return Regex.Replace(result, @"\{[A-Za-z0-9_]+\}", "—");
    }

    private static AlarmPushDto ToDto(Sys_Alarm alarm) => new()
    {
        AlarmId = alarm.Alarm_Id,
        Code = alarm.Code,
        Message = alarm.Message,
        Level = alarm.Level,
        Category = alarm.Category,
        Source = alarm.Source,
        DeviceName = alarm.DeviceName,
        Status = alarm.Status,
        AckUserName = alarm.AckUserName,
        AckDate = alarm.AckDate,
        CreateDate = alarm.CreateDate
    };
}
