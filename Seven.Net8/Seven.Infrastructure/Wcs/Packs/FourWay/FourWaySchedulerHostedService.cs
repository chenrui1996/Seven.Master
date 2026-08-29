using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>四向车包宿主占位：本阶段无物理通讯，仅标记包已启用。</summary>
public sealed class FourWaySchedulerHostedService : IHostedService
{
    private readonly ILogger<FourWaySchedulerHostedService> _logger;

    public FourWaySchedulerHostedService(ILogger<FourWaySchedulerHostedService> logger)
        => _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FourWay WCS pack scheduler started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FourWay WCS pack scheduler stopped");
        return Task.CompletedTask;
    }
}
