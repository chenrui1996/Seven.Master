using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Flow;
using Seven.Domain.Enums;
using Seven.Infrastructure.Mail;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;
namespace Seven.Business;

/// <summary>工作流引擎：定义、提交、多步审批、驳回</summary>
public class WorkFlowService : IWorkFlowService
{
    private static readonly System.Text.RegularExpressions.Regex SqlIdentifierRegex =
        new(@"^[A-Za-z_][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private readonly SevenDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _email;
    private readonly ILogger<WorkFlowService> _logger;

    public WorkFlowService(
        SevenDbContext db,
        ICurrentUserService currentUser,
        IEmailService email,
        ILogger<WorkFlowService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _email = email;
        _logger = logger;
    }

    public async Task<WebResponseContent> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Sys_WorkFlows.AsNoTracking()
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.CreateDate)
            .Select(f => new
            {
                f.WorkFlow_Id,
                f.WorkName,
                f.WorkTable,
                f.WorkTableKey,
                f.Enable,
                f.CreateDate,
                StepCount = f.Steps.Count(s => !s.IsDeleted),
            })
            .ToListAsync(cancellationToken);
        return WebResponseContent.Ok(data: list);
    }

    public async Task<WebResponseContent> GetDefinitionAsync(int workFlowId, CancellationToken cancellationToken = default)
    {
        var flow = await _db.Sys_WorkFlows.AsNoTracking()
            .Include(f => f.Steps.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(f => f.WorkFlow_Id == workFlowId && !f.IsDeleted, cancellationToken);
        if (flow == null) return WebResponseContent.Error("流程不存在");
        flow.Steps = flow.Steps.OrderBy(s => s.StepOrder).ToList();
        return WebResponseContent.Ok(data: flow);
    }

    public async Task<WebResponseContent> SaveDefinitionAsync(WorkFlowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.WorkName))
            return WebResponseContent.Error("流程名称不能为空");
        if (string.IsNullOrWhiteSpace(request.WorkTable))
            return WebResponseContent.Error("业务表名不能为空");

        Sys_WorkFlow flow;
        if (request.WorkFlow_Id > 0)
        {
            flow = await _db.Sys_WorkFlows.Include(f => f.Steps)
                .FirstOrDefaultAsync(f => f.WorkFlow_Id == request.WorkFlow_Id && !f.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("流程不存在");
            flow.WorkName = request.WorkName.Trim();
            flow.WorkTable = request.WorkTable.Trim();
            flow.WorkTableKey = string.IsNullOrWhiteSpace(request.WorkTableKey) ? "Id" : request.WorkTableKey.Trim();
            flow.Enable = request.Enable ?? 1;
            flow.ModifyDate = DateTime.Now;
            flow.Modifier = _currentUser.UserName;

            foreach (var old in flow.Steps)
                old.IsDeleted = true;
        }
        else
        {
            flow = new Sys_WorkFlow
            {
                WorkName = request.WorkName.Trim(),
                WorkTable = request.WorkTable.Trim(),
                WorkTableKey = string.IsNullOrWhiteSpace(request.WorkTableKey) ? "Id" : request.WorkTableKey.Trim(),
                Enable = request.Enable ?? 1,
                CreateDate = DateTime.Now,
                Creator = _currentUser.UserName,
                CreateId = _currentUser.UserId,
            };
            _db.Sys_WorkFlows.Add(flow);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var order = 1;
        foreach (var step in request.Steps.OrderBy(s => s.StepOrder))
        {
            _db.Sys_WorkFlowSteps.Add(new Sys_WorkFlowStep
            {
                WorkFlow_Id = flow.WorkFlow_Id,
                StepName = string.IsNullOrWhiteSpace(step.StepName) ? $"步骤{order}" : step.StepName.Trim(),
                StepOrder = order++,
                StepType = step.StepType ?? (int)WorkFlowStepType.Role,
                StepValue = step.StepValue,
                CreateDate = DateTime.Now,
                Creator = _currentUser.UserName,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("保存成功", new { flow.WorkFlow_Id });
    }

    public async Task<WebResponseContent> DeleteDefinitionsAsync(int[] ids, CancellationToken cancellationToken = default)
    {
        var flows = await _db.Sys_WorkFlows.Where(f => ids.Contains(f.WorkFlow_Id) && !f.IsDeleted).ToListAsync(cancellationToken);
        foreach (var f in flows)
        {
            f.IsDeleted = true;
            f.ModifyDate = DateTime.Now;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return WebResponseContent.Ok("删除成功");
    }

    public async Task<WebResponseContent> SubmitAsync(string tableName, string tableKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(tableKey))
            return WebResponseContent.Error("业务表或主键不能为空");

        var flow = await _db.Sys_WorkFlows
            .Include(f => f.Steps.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(f => f.WorkTable == tableName && f.Enable == 1 && !f.IsDeleted, cancellationToken);
        if (flow == null) return WebResponseContent.Error("未配置工作流");

        var steps = flow.Steps.OrderBy(s => s.StepOrder).ToList();
        if (steps.Count == 0) return WebResponseContent.Error("流程未配置步骤");

        var exists = await _db.Sys_WorkFlowTables.AnyAsync(t =>
            t.WorkTable == tableName
            && t.WorkTableKey == tableKey
            && (t.AuditStatus == (int)AuditStatus.Pending || t.AuditStatus == (int)AuditStatus.InProgress)
            && !t.IsDeleted, cancellationToken);
        if (exists) return WebResponseContent.Error("该单据已在审批中");

        var first = steps[0];
        var instance = new Sys_WorkFlowTable
        {
            WorkFlow_Id = flow.WorkFlow_Id,
            WorkTable = tableName,
            WorkTableKey = tableKey,
            AuditStatus = (int)AuditStatus.InProgress,
            CurrentStepId = first.WorkStepFlow_Id,
            CreateDate = DateTime.Now,
            Creator = _currentUser.UserName,
            CreateId = _currentUser.UserId,
        };
        _db.Sys_WorkFlowTables.Add(instance);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var step in steps)
        {
            _db.Sys_WorkFlowTableSteps.Add(new Sys_WorkFlowTableStep
            {
                WorkFlowTable_Id = instance.WorkFlowTable_Id,
                WorkStepFlow_Id = step.WorkStepFlow_Id,
                AuditStatus = step.WorkStepFlow_Id == first.WorkStepFlow_Id
                    ? (int)AuditStatus.Pending
                    : (int)AuditStatus.Pending,
                CreateDate = DateTime.Now,
            });
        }

        await TryWriteBusinessAuditStatusAsync(tableName, tableKey, (int)AuditStatus.InProgress, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流已提交",
            $"您提交的业务单据已进入审批流程（表 {tableName}，主键 {tableKey}）。", cancellationToken);
        return WebResponseContent.Ok("提交审批成功", instance);
    }

    public async Task<WebResponseContent> AuditAsync(int workFlowTableId, int auditStatus, string? remark, CancellationToken cancellationToken = default)
    {
        if (auditStatus is not ((int)AuditStatus.Approved or (int)AuditStatus.Returned or (int)AuditStatus.Rejected))
            return WebResponseContent.Error("无效的审批结果");

        var instance = await _db.Sys_WorkFlowTables
            .FirstOrDefaultAsync(t => t.WorkFlowTable_Id == workFlowTableId && !t.IsDeleted, cancellationToken);
        if (instance == null) return WebResponseContent.Error("流程实例不存在");
        if (instance.AuditStatus is (int)AuditStatus.Approved or (int)AuditStatus.Rejected)
            return WebResponseContent.Error("流程已结束");

        var currentStepId = instance.CurrentStepId;
        if (currentStepId == null) return WebResponseContent.Error("当前步骤无效");

        if (!await CanCurrentUserAuditAsync(currentStepId.Value, cancellationToken))
            return WebResponseContent.Error("无权审批当前步骤");

        var tableStep = await _db.Sys_WorkFlowTableSteps
            .FirstOrDefaultAsync(s => s.WorkFlowTable_Id == workFlowTableId
                && s.WorkStepFlow_Id == currentStepId && !s.IsDeleted, cancellationToken);
        if (tableStep != null)
        {
            tableStep.AuditStatus = auditStatus;
            tableStep.AuditUserId = _currentUser.UserId;
            tableStep.AuditDate = DateTime.Now;
            tableStep.ModifyDate = DateTime.Now;
        }

        _db.Sys_WorkFlowTableAuditLogs.Add(new Sys_WorkFlowTableAuditLog
        {
            WorkFlowTable_Id = workFlowTableId,
            WorkStepFlow_Id = currentStepId.Value,
            AuditStatus = auditStatus,
            Remark = remark,
            AuditUser = _currentUser.UserName,
            CreateDate = DateTime.Now,
        });

        var flowSteps = await _db.Sys_WorkFlowSteps.AsNoTracking()
            .Where(s => s.WorkFlow_Id == instance.WorkFlow_Id && !s.IsDeleted)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);
        var idx = flowSteps.FindIndex(s => s.WorkStepFlow_Id == currentStepId.Value);

        if (auditStatus == (int)AuditStatus.Rejected)
        {
            instance.AuditStatus = auditStatus;
            instance.CurrentStepId = null;
            instance.ModifyDate = DateTime.Now;
            await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, auditStatus, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批结果：已拒绝",
                $"您的单据审批已被拒绝。意见：{remark ?? "无"}", cancellationToken);
            return WebResponseContent.Ok("已拒绝");
        }

        if (auditStatus == (int)AuditStatus.Returned)
        {
            if (idx > 0)
            {
                var prevStepId = flowSteps[idx - 1].WorkStepFlow_Id;
                instance.CurrentStepId = prevStepId;
                instance.AuditStatus = (int)AuditStatus.InProgress;
                instance.ModifyDate = DateTime.Now;

                var prevTableStep = await _db.Sys_WorkFlowTableSteps
                    .FirstOrDefaultAsync(s => s.WorkFlowTable_Id == workFlowTableId
                        && s.WorkStepFlow_Id == prevStepId && !s.IsDeleted, cancellationToken);
                ResetTableStepToPending(prevTableStep);
                ResetTableStepToPending(tableStep);

                await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, (int)AuditStatus.InProgress, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批：已驳回",
                    $"您的单据已被驳回至上一步。意见：{remark ?? "无"}", cancellationToken);
                return WebResponseContent.Ok("已驳回至上一步");
            }

            instance.AuditStatus = auditStatus;
            instance.CurrentStepId = null;
            instance.ModifyDate = DateTime.Now;
            await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, auditStatus, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批：已驳回",
                $"您的单据已被驳回。意见：{remark ?? "无"}", cancellationToken);
            return WebResponseContent.Ok("已驳回");
        }

        // 通过 → 下一步或结束
        if (idx < 0 || idx >= flowSteps.Count - 1)
        {
            instance.AuditStatus = (int)AuditStatus.Approved;
            instance.CurrentStepId = null;
            instance.ModifyDate = DateTime.Now;
            await TryWriteBusinessAuditStatusAsync(instance.WorkTable, instance.WorkTableKey, (int)AuditStatus.Approved, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批完成",
                "您的单据已全部审批通过。", cancellationToken);
            return WebResponseContent.Ok("审批完成（全部通过）");
        }

        var next = flowSteps[idx + 1];
        instance.CurrentStepId = next.WorkStepFlow_Id;
        instance.AuditStatus = (int)AuditStatus.InProgress;
        instance.ModifyDate = DateTime.Now;

        var nextTableStep = await _db.Sys_WorkFlowTableSteps
            .FirstOrDefaultAsync(s => s.WorkFlowTable_Id == workFlowTableId
                && s.WorkStepFlow_Id == next.WorkStepFlow_Id && !s.IsDeleted, cancellationToken);
        if (nextTableStep != null && nextTableStep.AuditStatus != (int)AuditStatus.Pending)
            ResetTableStepToPending(nextTableStep);

        await _db.SaveChangesAsync(cancellationToken);
        await TryNotifyWorkFlowEmailAsync(instance.CreateId, "工作流审批：已通过",
            "您的单据当前步骤已通过，已进入下一步审批。", cancellationToken);
        return WebResponseContent.Ok("已通过，进入下一步");
    }

    static void ResetTableStepToPending(Sys_WorkFlowTableStep? step)
    {
        if (step == null) return;
        step.AuditStatus = (int)AuditStatus.Pending;
        step.AuditUserId = null;
        step.AuditDate = null;
        step.ModifyDate = DateTime.Now;
    }

    public async Task<PageGridData<Sys_WorkFlowTable>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Sys_WorkFlowTables.AsNoTracking().Where(t => !t.IsDeleted);
        var filter = ReadWhereValue(options.Wheres, "scope") ?? ReadWhereValue(options.Wheres, "filter");

        if (string.Equals(filter, "todo", StringComparison.OrdinalIgnoreCase) && _currentUser.UserId is int uid)
        {
            var stepIds = await GetAuditableStepIdsAsync(uid, _currentUser.RoleId, cancellationToken);
            query = query.Where(t =>
                (t.AuditStatus == (int)AuditStatus.Pending || t.AuditStatus == (int)AuditStatus.InProgress)
                && t.CurrentStepId != null
                && stepIds.Contains(t.CurrentStepId.Value));
        }
        else if (string.Equals(filter, "done", StringComparison.OrdinalIgnoreCase) && _currentUser.UserId is int uid2)
        {
            var doneIds = await _db.Sys_WorkFlowTableAuditLogs.AsNoTracking()
                .Where(l => l.AuditUser == _currentUser.UserName)
                .Select(l => l.WorkFlowTable_Id)
                .Distinct()
                .ToListAsync(cancellationToken);
            query = query.Where(t => doneIds.Contains(t.WorkFlowTable_Id));
        }

        query = query.OrderByDescending(w => w.CreateDate);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    async Task<bool> CanCurrentUserAuditAsync(int stepId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int uid) return false;
        var step = await _db.Sys_WorkFlowSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkStepFlow_Id == stepId && !s.IsDeleted, ct);
        if (step == null) return false;

        // 超级管理员（角色名或 RoleId=1）放行
        if (_currentUser.RoleId == 1) return true;

        var type = (WorkFlowStepType)(step.StepType ?? (int)WorkFlowStepType.Role);
        var value = step.StepValue ?? string.Empty;
        return type switch
        {
            WorkFlowStepType.User => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => int.TryParse(v, out var id) && id == uid),
            WorkFlowStepType.Role => _currentUser.RoleId is int rid
                && value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(v => int.TryParse(v, out var id) && id == rid),
            WorkFlowStepType.Department => await UserInDepartmentsAsync(uid, value, ct),
            _ => false,
        };
    }

    async Task<List<int>> GetAuditableStepIdsAsync(int userId, int? roleId, CancellationToken ct)
    {
        var steps = await _db.Sys_WorkFlowSteps.AsNoTracking().Where(s => !s.IsDeleted).ToListAsync(ct);
        var result = new List<int>();
        foreach (var step in steps)
        {
            var type = (WorkFlowStepType)(step.StepType ?? (int)WorkFlowStepType.Role);
            var value = step.StepValue ?? string.Empty;
            var ok = type switch
            {
                WorkFlowStepType.User => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(v => int.TryParse(v, out var id) && id == userId),
                WorkFlowStepType.Role => roleId is int rid
                    && value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Any(v => int.TryParse(v, out var id) && id == rid),
                WorkFlowStepType.Department => await UserInDepartmentsAsync(userId, value, ct),
                _ => false,
            };
            if (ok || roleId == 1) result.Add(step.WorkStepFlow_Id);
        }
        return result.Distinct().ToList();
    }

    async Task<bool> UserInDepartmentsAsync(int userId, string deptCsv, CancellationToken ct)
    {
        var deptIds = deptCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => int.TryParse(v, out var id) ? id : 0).Where(id => id > 0).ToHashSet();
        if (deptIds.Count == 0) return false;

        var user = await _db.Sys_Users.AsNoTracking().FirstOrDefaultAsync(u => u.User_Id == userId, ct);
        if (user?.DeptIds != null)
        {
            var userDepts = user.DeptIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => int.TryParse(v, out var id) ? id : 0).Where(id => id > 0);
            if (userDepts.Any(deptIds.Contains)) return true;
        }

        return await _db.Sys_UserDepartments.AsNoTracking()
            .AnyAsync(ud => ud.UserId == userId && deptIds.Contains(ud.DepartmentId) && !ud.IsDeleted, ct);
    }

    static string? ReadWhereValue(string? wheresJson, string name)
    {
        if (string.IsNullOrWhiteSpace(wheresJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(wheresJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var n)) continue;
                if (!string.Equals(n.GetString(), name, StringComparison.OrdinalIgnoreCase)) continue;
                if (!item.TryGetProperty("value", out var v)) return null;
                return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
            }
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>若业务表存在 AuditStatus 列则回写（原生 SQL，忽略失败）</summary>
    async Task TryWriteBusinessAuditStatusAsync(string? tableName, string? tableKey, int status, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(tableKey)) return;
        if (!SqlIdentifierRegex.IsMatch(tableName)) return;

        var keyCol = await _db.Sys_WorkFlows.AsNoTracking()
            .Where(f => f.WorkTable == tableName && !f.IsDeleted)
            .Select(f => f.WorkTableKey)
            .FirstOrDefaultAsync(ct) ?? "Id";
        keyCol = string.IsNullOrWhiteSpace(keyCol) ? "Id" : keyCol.Trim();
        if (!SqlIdentifierRegex.IsMatch(keyCol)) return;

        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                $"UPDATE `{tableName}` SET `AuditStatus` = {{0}} WHERE CAST(`{keyCol}` AS CHAR) = {{1}}",
                [status, tableKey],
                ct);
        }
        catch
        {
            // 业务表可能没有 AuditStatus / 主键列，忽略
        }
    }

    async Task TryNotifyWorkFlowEmailAsync(int? creatorUserId, string subject, string htmlBody, CancellationToken ct)
    {
        if (creatorUserId is not int uid || uid <= 0) return;

        try
        {
            var email = await _db.Sys_Users.AsNoTracking()
                .Where(u => u.User_Id == uid)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(email)) return;

            var result = await _email.SendAsync(email, subject, $"<p>{htmlBody}</p>", ct);
            if (!result.Status)
                _logger.LogDebug("工作流邮件未发送 UserId={UserId} Reason={Reason}", uid, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "工作流邮件发送异常 UserId={UserId}", creatorUserId);
        }
    }
}
