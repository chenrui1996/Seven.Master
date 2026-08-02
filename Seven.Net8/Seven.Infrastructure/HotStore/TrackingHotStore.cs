using System.Threading.Channels;
using Seven.Application.Interfaces;

namespace Seven.Infrastructure.HotStore;

/// <summary>
/// 装饰器：透传读写，并将 Set/Remove/Acquire/Release 写入脏数据通道供异步落库。
/// </summary>
public sealed class TrackingHotStore : IHotStore
{
    private readonly IHotStore _inner;
    private readonly ChannelWriter<HotStoreChange> _writer;
    private readonly Action? _markReady;

    public TrackingHotStore(IHotStore inner, ChannelWriter<HotStoreChange> writer, Action? markReady = null)
    {
        _inner = inner;
        _writer = writer;
        _markReady = markReady;
    }

    /// <inheritdoc />
    public bool IsReady => _inner.IsReady;

    /// <summary>预热完成后由宿主标记就绪</summary>
    public void MarkReady()
    {
        _markReady?.Invoke();
        if (_inner is MemoryHotStore mem) mem.MarkReady();
        else if (_inner is RedisHotStore redis) redis.MarkReady();
    }

    /// <inheritdoc />
    public Task WarmupAsync(CancellationToken cancellationToken = default) =>
        _inner.WarmupAsync(cancellationToken);

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        _inner.GetAsync<T>(key, cancellationToken);

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        await _inner.SetAsync(key, value, ttl, cancellationToken).ConfigureAwait(false);
        await PublishAsync(new HotStoreChange(key, HotStoreChangeKind.Set, value, null, DateTimeOffset.UtcNow), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryUpdateAsync<T>(string key, Func<T?, T> updater, CancellationToken cancellationToken = default)
    {
        T? snapshot = default;
        var ok = await _inner.TryUpdateAsync<T>(key, current =>
        {
            var next = updater(current);
            snapshot = next;
            return next;
        }, cancellationToken).ConfigureAwait(false);

        if (ok)
        {
            await PublishAsync(
                new HotStoreChange(key, HotStoreChangeKind.Set, snapshot, null, DateTimeOffset.UtcNow),
                cancellationToken).ConfigureAwait(false);
        }

        return ok;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _inner.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        await PublishAsync(new HotStoreChange(key, HotStoreChangeKind.Remove, null, null, DateTimeOffset.UtcNow), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(string resourceKey, string ownerId, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var ok = await _inner.TryAcquireAsync(resourceKey, ownerId, ttl, cancellationToken).ConfigureAwait(false);
        if (ok)
        {
            await PublishAsync(
                new HotStoreChange(resourceKey, HotStoreChangeKind.Acquire, null, ownerId, DateTimeOffset.UtcNow),
                cancellationToken).ConfigureAwait(false);
        }

        return ok;
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(string resourceKey, string ownerId, CancellationToken cancellationToken = default)
    {
        await _inner.ReleaseAsync(resourceKey, ownerId, cancellationToken).ConfigureAwait(false);
        await PublishAsync(
            new HotStoreChange(resourceKey, HotStoreChangeKind.Release, null, ownerId, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishAsync(HotStoreChange change, CancellationToken cancellationToken)
    {
        try
        {
            await _writer.WriteAsync(change, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            // 宿主已停止
        }
    }
}
