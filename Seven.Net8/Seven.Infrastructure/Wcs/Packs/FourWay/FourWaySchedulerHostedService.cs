using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>薄宿主：端口事件 → Scoped DestinationService；周期重试 Routing 占边 + 唤醒停车/Hoist 挂起。</summary>
public sealed class FourWaySchedulerHostedService : BackgroundService
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(3);

    private readonly IEquipmentTriggerPort _port;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FourWaySchedulerHostedService> _logger;

    public FourWaySchedulerHostedService(
        IEquipmentTriggerPort port,
        IServiceScopeFactory scopeFactory,
        ILogger<FourWaySchedulerHostedService> logger)
    {
        _port = port;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _port.DestinationRequested += OnDestinationRequestedAsync;
        _port.SegmentFeedback += OnSegmentFeedbackAsync;
        _logger.LogInformation("FourWay destination subscriptions started");

        try
        {
            // 启动即扫一次 Suspended/Queued Hoist Exec（再进入周期）
            await RunPeriodicScanAsync(stoppingToken, startup: true);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(RetryInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await RunPeriodicScanAsync(stoppingToken, startup: false);
            }
        }
        finally
        {
            _port.DestinationRequested -= OnDestinationRequestedAsync;
            _port.SegmentFeedback -= OnSegmentFeedbackAsync;
        }
    }

    private async Task RunPeriodicScanAsync(CancellationToken stoppingToken, bool startup)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var path = scope.ServiceProvider.GetRequiredService<FourWayPathDispatcher>();
            var pack = scope.ServiceProvider.GetRequiredService<FourWayWcsPack>();
            var hoist = scope.ServiceProvider.GetRequiredService<FourWayHoistOrchestrator>();

            var resumed = await path.RetryStuckRoutingAsync(stoppingToken);
            if (resumed > 0)
                _logger.LogInformation("FourWay path grant retry resumed {Count} shuttle(s)", resumed);

            var promoted = await pack.PromoteRetrievalsAfterPathGrantAsync(stoppingToken);
            if (promoted > 0)
                _logger.LogInformation("FourWay parking promoted {Count} retrieval(s) after grant", promoted);

            var woken = await pack.WakeSuspendedRetrievalsAsync(stoppingToken);
            if (woken > 0)
                _logger.LogInformation("FourWay parking wake dispatched {Count} retrieval(s)", woken);

            var hoistWoken = await hoist.ScanAndWakeQueuedExecsAsync(stoppingToken);
            if (hoistWoken > 0 || startup)
                _logger.LogInformation(
                    "FourWay hoist scan woke {Count} exec(s){Startup}",
                    hoistWoken,
                    startup ? " (startup)" : "");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ignore
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FourWay path/parking/hoist retry scan failed");
        }
    }

    private async Task OnDestinationRequestedAsync(DestinationRequestTrigger trigger)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dest = scope.ServiceProvider.GetRequiredService<FourWayDestinationService>();
            await dest.HandleDestinationRequestedAsync(trigger);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FourWay DestinationRequested handling failed for {Container}", trigger.ContainerCode);
        }
    }

    private async Task OnSegmentFeedbackAsync(DeviceSegmentFeedback feedback)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dest = scope.ServiceProvider.GetRequiredService<FourWayDestinationService>();
            await dest.HandleSegmentFeedbackAsync(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FourWay SegmentFeedback handling failed for {Container}", feedback.ContainerCode);
        }
    }
}
