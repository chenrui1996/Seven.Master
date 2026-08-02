using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seven.Application.Interfaces;

namespace Seven.Infrastructure.HotStore.Demo;

/// <summary>
/// 四向车 Demo 调度节拍：就绪后循环占用/释放节点，演示热路径不打库。
/// </summary>
public sealed class WcsDemoSchedulerHostedService : BackgroundService
{
    private readonly IHotStore _store;
    private readonly ILogger<WcsDemoSchedulerHostedService> _logger;

    public WcsDemoSchedulerHostedService(IHotStore store, ILogger<WcsDemoSchedulerHostedService> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && !_store.IsReady)
            await Task.Delay(100, stoppingToken).ConfigureAwait(false);

        if (stoppingToken.IsCancellationRequested) return;

        _logger.LogInformation("WCS demo scheduler started");
        var vehicleId = "V-DEMO-1";
        var path = new[] { "N1", "N2", "N3", "N4", "N5" };
        var index = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var node = path[index % path.Length];
                var lockKey = WcsHotKeys.Lock(node);
                var acquired = await _store.TryAcquireAsync(lockKey, vehicleId, TimeSpan.FromSeconds(5), stoppingToken)
                    .ConfigureAwait(false);

                if (acquired)
                {
                    await _store.TryUpdateAsync<WcsNodeState>(WcsHotKeys.Node(node), current =>
                    {
                        current ??= new WcsNodeState { NodeId = node };
                        current.Free = false;
                        current.OwnerId = vehicleId;
                        return current;
                    }, stoppingToken).ConfigureAwait(false);

                    await Task.Delay(200, stoppingToken).ConfigureAwait(false);

                    await _store.ReleaseAsync(lockKey, vehicleId, stoppingToken).ConfigureAwait(false);
                    await _store.TryUpdateAsync<WcsNodeState>(WcsHotKeys.Node(node), current =>
                    {
                        current ??= new WcsNodeState { NodeId = node };
                        current.Free = true;
                        current.OwnerId = null;
                        return current;
                    }, stoppingToken).ConfigureAwait(false);

                    index++;
                }
                else
                {
                    _logger.LogDebug("WCS demo: node {Node} busy, retry", node);
                }

                await Task.Delay(300, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "WCS demo scheduler tick failed");
                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
