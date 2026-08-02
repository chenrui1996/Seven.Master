using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.HotStore;

/// <summary>启动预热 + 按节拍批量落库</summary>
public sealed class HotStoreHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IHotStore _store;
    private readonly ChannelReader<HotStoreChange> _reader;
    private readonly HotStoreOptions _options;
    private readonly ILogger<HotStoreHostedService> _logger;

    public HotStoreHostedService(
        IServiceProvider services,
        IHotStore store,
        ChannelReader<HotStoreChange> reader,
        IOptions<HotStoreOptions> options,
        ILogger<HotStoreHostedService> logger)
    {
        _services = services;
        _store = store;
        _reader = reader;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.WarmupOnStartup)
            await RunWarmupAsync(stoppingToken).ConfigureAwait(false);
        else
            MarkReady();

        if (!_options.PersistEnabled)
        {
            await DrainDiscardAsync(stoppingToken).ConfigureAwait(false);
            return;
        }

        var interval = Math.Max(50, _options.PersistIntervalMs);
        var batchSize = Math.Max(1, _options.PersistBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                var batch = await ReadBatchAsync(batchSize, stoppingToken).ConfigureAwait(false);
                if (batch.Count == 0) continue;
                await PersistAsync(batch, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HotStore persist loop failed");
            }
        }
    }

    private async Task RunWarmupAsync(CancellationToken stoppingToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.WarmupTimeoutSeconds)));

        try
        {
            using var scope = _services.CreateScope();
            var warmups = scope.ServiceProvider.GetServices<IHotStoreWarmup>().ToList();
            foreach (var warmup in warmups)
            {
                _logger.LogInformation("HotStore warmup starting: {Name}", warmup.Name);
                await warmup.WarmupAsync(_store, timeoutCts.Token).ConfigureAwait(false);
                _logger.LogInformation("HotStore warmup finished: {Name}", warmup.Name);
            }

            MarkReady();
            _logger.LogInformation("HotStore is ready ({Count} warmup steps)", warmups.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "HotStore warmup failed; marking ready anyway to avoid permanent stall");
            MarkReady();
        }
    }

    private void MarkReady()
    {
        if (_store is TrackingHotStore tracking)
            tracking.MarkReady();
        else if (_store is MemoryHotStore mem)
            mem.MarkReady();
        else if (_store is RedisHotStore redis)
            redis.MarkReady();
    }

    private Task<List<HotStoreChange>> ReadBatchAsync(int batchSize, CancellationToken ct)
    {
        var batch = new List<HotStoreChange>(batchSize);
        while (batch.Count < batchSize && _reader.TryRead(out var item))
            batch.Add(item);
        return Task.FromResult(batch);
    }

    private async Task PersistAsync(List<HotStoreChange> batch, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var persisters = scope.ServiceProvider.GetServices<IHotStorePersister>().ToList();
        if (persisters.Count == 0)
        {
            _logger.LogDebug("HotStore dirty batch {Count} dropped (no persisters)", batch.Count);
            return;
        }

        foreach (var persister in persisters)
        {
            try
            {
                await persister.PersistBatchAsync(batch, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HotStore persister {Name} failed for batch {Count}", persister.Name, batch.Count);
            }
        }
    }

    private async Task DrainDiscardAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var _ in _reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                // PersistEnabled=false：丢弃脏标记
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }
}
