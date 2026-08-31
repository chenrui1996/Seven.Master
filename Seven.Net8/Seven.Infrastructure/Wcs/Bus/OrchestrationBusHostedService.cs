using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Seven.Infrastructure.Wcs.Bus;

/// <summary>V1 薄宿主：OnLegEvent 已推进下一段；此处仅恢复卡住的 Pending。</summary>
public sealed class OrchestrationBusHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrchestrationBusHostedService> _logger;

    public OrchestrationBusHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrchestrationBusHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrchestrationBus hosted service started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<OrchestrationBus>();
                await bus.ActivateDuePendingLegsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OrchestrationBus pending-leg scan failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
