using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>薄宿主：把 Singleton 端口事件转到 Scoped DestinationService。</summary>
public sealed class StackerSchedulerHostedService : IHostedService
{
    private readonly IEquipmentTriggerPort _port;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StackerSchedulerHostedService> _logger;

    public StackerSchedulerHostedService(
        IEquipmentTriggerPort port,
        IServiceScopeFactory scopeFactory,
        ILogger<StackerSchedulerHostedService> logger)
    {
        _port = port;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _port.DestinationRequested += OnDestinationRequestedAsync;
        _port.SegmentFeedback += OnSegmentFeedbackAsync;
        _logger.LogInformation("Stacker destination subscriptions started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _port.DestinationRequested -= OnDestinationRequestedAsync;
        _port.SegmentFeedback -= OnSegmentFeedbackAsync;
        return Task.CompletedTask;
    }

    private async Task OnDestinationRequestedAsync(DestinationRequestTrigger trigger)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dest = scope.ServiceProvider.GetRequiredService<StackerDestinationService>();
            await dest.HandleDestinationRequestedAsync(trigger);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stacker DestinationRequested handling failed for {Container}", trigger.ContainerCode);
        }
    }

    private async Task OnSegmentFeedbackAsync(DeviceSegmentFeedback feedback)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dest = scope.ServiceProvider.GetRequiredService<StackerDestinationService>();
            await dest.HandleSegmentFeedbackAsync(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stacker SegmentFeedback handling failed for {Container}", feedback.ContainerCode);
        }
    }
}
