using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Quartz;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Quartz;

/// <summary>Quartz DI 与动态任务调度</summary>
public static class QuartzHostedSetup
{
    public static IServiceCollection AddSevenQuartz(this IServiceCollection services)
    {
        services.AddScoped<IQuartzJobService, QuartzJobService>();
        services.AddQuartz();
        services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
        services.AddHostedService<QuartzJobBootstrapper>();
        return services;
    }

    /// <summary>功能关闭时注册占位实现，避免 Controller 构造失败</summary>
    public static IServiceCollection AddSevenQuartzStub(this IServiceCollection services)
    {
        services.AddScoped<IQuartzJobService, DisabledQuartzJobService>();
        return services;
    }
}

/// <summary>Quartz 关闭时的占位服务</summary>
public sealed class DisabledQuartzJobService : IQuartzJobService
{
    private static WebResponseContent Off() => WebResponseContent.Error("功能未启用: Quartz");

    public Task<PageGridData<Sys_QuartzOptions>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        Task.FromResult(new PageGridData<Sys_QuartzOptions> { Rows = [], Total = 0 });

    public Task<WebResponseContent> SaveAsync(Sys_QuartzOptions entity, CancellationToken ct = default) => Task.FromResult(Off());
    public Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken ct = default) => Task.FromResult(Off());
    public Task<WebResponseContent> SetEnableAsync(int id, bool enable, CancellationToken ct = default) => Task.FromResult(Off());
    public Task<WebResponseContent> RunNowAsync(int id, CancellationToken ct = default) => Task.FromResult(Off());

    public Task<PageGridData<Sys_QuartzLog>> GetLogPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        Task.FromResult(new PageGridData<Sys_QuartzLog> { Rows = [], Total = 0 });

    public Task SyncSchedulerAsync(CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>定时任务管理</summary>
public interface IQuartzJobService
{
    Task<PageGridData<Sys_QuartzOptions>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WebResponseContent> SaveAsync(Sys_QuartzOptions entity, CancellationToken ct = default);
    Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken ct = default);
    Task<WebResponseContent> SetEnableAsync(int id, bool enable, CancellationToken ct = default);
    Task<WebResponseContent> RunNowAsync(int id, CancellationToken ct = default);
    Task<PageGridData<Sys_QuartzLog>> GetLogPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task SyncSchedulerAsync(CancellationToken ct = default);
}

public class QuartzJobService : IQuartzJobService
{
    private readonly SevenDbContext _db;
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzJobService(SevenDbContext db, ISchedulerFactory schedulerFactory)
    {
        _db = db;
        _schedulerFactory = schedulerFactory;
    }

    public async Task<PageGridData<Sys_QuartzOptions>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default)
    {
        var query = _db.Sys_QuartzOptions.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Id);
        return await Services.CrudHelper.PaginateAsync(query, options, ct);
    }

    public async Task<WebResponseContent> SaveAsync(Sys_QuartzOptions entity, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entity.TaskName) || string.IsNullOrWhiteSpace(entity.CronExpression))
            return WebResponseContent.Error("任务名与 Cron 不能为空");

        if (entity.Id > 0)
        {
            var existing = await _db.Sys_QuartzOptions.FirstOrDefaultAsync(x => x.Id == entity.Id && !x.IsDeleted, ct);
            if (existing == null) return WebResponseContent.Error("任务不存在");
            existing.TaskName = entity.TaskName.Trim();
            existing.GroupName = entity.GroupName ?? "DEFAULT";
            existing.CronExpression = entity.CronExpression.Trim();
            existing.ApiUrl = entity.ApiUrl;
            existing.Enable = entity.Enable ?? 1;
            existing.ModifyDate = DateTime.Now;
        }
        else
        {
            entity.GroupName ??= "DEFAULT";
            entity.CreateDate = DateTime.Now;
            entity.Enable ??= 1;
            _db.Sys_QuartzOptions.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
        await SyncSchedulerAsync(ct);
        return WebResponseContent.Ok("保存成功", entity);
    }

    public async Task<WebResponseContent> DeleteAsync(int[] ids, CancellationToken ct = default)
    {
        var list = await _db.Sys_QuartzOptions.Where(x => ids.Contains(x.Id) && !x.IsDeleted).ToListAsync(ct);
        foreach (var item in list) item.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        await SyncSchedulerAsync(ct);
        return WebResponseContent.Ok("删除成功");
    }

    public async Task<WebResponseContent> SetEnableAsync(int id, bool enable, CancellationToken ct = default)
    {
        var item = await _db.Sys_QuartzOptions.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (item == null) return WebResponseContent.Error("任务不存在");
        item.Enable = (byte)(enable ? 1 : 0);
        item.ModifyDate = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        await SyncSchedulerAsync(ct);
        return WebResponseContent.Ok(enable ? "已启用" : "已停用");
    }

    public async Task<WebResponseContent> RunNowAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.Sys_QuartzOptions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (item == null) return WebResponseContent.Error("任务不存在");
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var key = new JobKey($"job-{id}", item.GroupName ?? "DEFAULT");
        if (!await scheduler.CheckExists(key, ct))
        {
            await ScheduleOneAsync(scheduler, item, ct);
        }
        await scheduler.TriggerJob(key, ct);
        return WebResponseContent.Ok("已触发执行");
    }

    public async Task<PageGridData<Sys_QuartzLog>> GetLogPageDataAsync(PageDataOptions options, CancellationToken ct = default)
    {
        var query = _db.Sys_QuartzLogs.AsNoTracking().OrderByDescending(x => x.CreateDate);
        return await Services.CrudHelper.PaginateAsync(query, options, ct);
    }

    public async Task SyncSchedulerAsync(CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var jobs = await _db.Sys_QuartzOptions.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(ct);
        foreach (var job in jobs)
        {
            var key = new JobKey($"job-{job.Id}", job.GroupName ?? "DEFAULT");
            if (await scheduler.CheckExists(key, ct))
                await scheduler.DeleteJob(key, ct);
            if (job.Enable == 1)
                await ScheduleOneAsync(scheduler, job, ct);
        }
    }

    static async Task ScheduleOneAsync(IScheduler scheduler, Sys_QuartzOptions job, CancellationToken ct)
    {
        if (!CronExpression.IsValidExpression(job.CronExpression)) return;
        var detail = JobBuilder.Create<HttpCallbackJob>()
            .WithIdentity($"job-{job.Id}", job.GroupName ?? "DEFAULT")
            .UsingJobData("taskId", job.Id)
            .UsingJobData("apiUrl", job.ApiUrl ?? "")
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trg-{job.Id}", job.GroupName ?? "DEFAULT")
            .WithCronSchedule(job.CronExpression)
            .Build();
        await scheduler.ScheduleJob(detail, trigger, ct);
    }
}

/// <summary>启动时同步任务到调度器</summary>
public class QuartzJobBootstrapper : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<QuartzJobBootstrapper> _logger;

    public QuartzJobBootstrapper(IServiceProvider sp, ILogger<QuartzJobBootstrapper> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IQuartzJobService>();
            await svc.SyncSchedulerAsync(cancellationToken);
            _logger.LogInformation("Quartz 任务已同步");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Quartz 任务同步失败（库可能尚未初始化）");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>示例 Job：清理 refresh 缓存前缀 + 可选 HTTP 回调</summary>
public class HttpCallbackJob : IJob
{
    private readonly IServiceProvider _sp;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpCallbackJob> _logger;

    public HttpCallbackJob(IServiceProvider sp, IHttpClientFactory httpClientFactory, ILogger<HttpCallbackJob> logger)
    {
        _sp = sp;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var taskId = context.MergedJobDataMap.GetInt("taskId");
        var apiUrl = context.MergedJobDataMap.GetString("apiUrl") ?? "";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var success = true;
        var content = "ok";
        try
        {
            if (string.IsNullOrWhiteSpace(apiUrl) || apiUrl.Equals("cleanup-refresh", StringComparison.OrdinalIgnoreCase))
            {
                // 示例：无外部 URL 时仅记日志（Refresh Token 依赖缓存 TTL 自然过期）
                content = $"cleanup-refresh tick at {DateTime.Now:O}";
                _logger.LogInformation("Quartz 示例任务执行: {Content}", content);
            }
            else if (Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
            {
                var client = _httpClientFactory.CreateClient("quartz");
                var resp = await client.GetAsync(uri, context.CancellationToken);
                content = $"{(int)resp.StatusCode} {await resp.Content.ReadAsStringAsync(context.CancellationToken)}";
                success = resp.IsSuccessStatusCode;
            }
        }
        catch (Exception ex)
        {
            success = false;
            content = ex.Message;
            _logger.LogError(ex, "Quartz 任务 {TaskId} 失败", taskId);
        }
        finally
        {
            sw.Stop();
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SevenDbContext>();
                db.Sys_QuartzLogs.Add(new Sys_QuartzLog
                {
                    TaskId = taskId,
                    Success = success,
                    ResponseContent = content.Length > 2000 ? content[..2000] : content,
                    ElapsedMs = (int)sw.ElapsedMilliseconds,
                    CreateDate = DateTime.Now,
                });
                await db.SaveChangesAsync(context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "写入 Quartz 日志失败");
            }
        }
    }
}
