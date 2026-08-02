using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.HotStore;

/// <summary>进程内热存储（强类型对象，无 JSON 往返）</summary>
public sealed class MemoryHotStore : IHotStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _locks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> _keyGates = new(StringComparer.Ordinal);
    private readonly HotStoreOptions _options;
    private volatile bool _ready;

    public MemoryHotStore(IOptions<HotStoreOptions> options) => _options = options.Value;

    /// <inheritdoc />
    public bool IsReady => _ready;

    /// <summary>预热完成后由宿主标记就绪</summary>
    public void MarkReady() => _ready = true;

    /// <inheritdoc />
    public Task WarmupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var full = Prefixed(key);
        if (!_entries.TryGetValue(full, out var entry) || entry.IsExpired)
        {
            if (entry?.IsExpired == true) _entries.TryRemove(full, out _);
            return Task.FromResult(default(T));
        }

        return Task.FromResult(entry.Value is T typed ? typed : default);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var full = Prefixed(key);
        _entries[full] = new Entry(value!, ResolveExpiry(ttl));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> TryUpdateAsync<T>(string key, Func<T?, T> updater, CancellationToken cancellationToken = default)
    {
        var full = Prefixed(key);
        var gate = _keyGates.GetOrAdd(full, _ => new object());
        lock (gate)
        {
            T? current = default;
            if (_entries.TryGetValue(full, out var entry) && !entry.IsExpired && entry.Value is T typed)
                current = typed;
            else if (entry?.IsExpired == true)
                _entries.TryRemove(full, out _);

            var next = updater(current);
            _entries[full] = new Entry(next!, ResolveExpiry(null));
        }

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _entries.TryRemove(Prefixed(key), out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(string resourceKey, string ownerId, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var full = Prefixed(resourceKey);
        var expiry = ResolveExpiry(ttl);

        while (true)
        {
            if (_locks.TryGetValue(full, out var existing))
            {
                if (existing == ownerId)
                {
                    TouchLock(full, ownerId, expiry);
                    return Task.FromResult(true);
                }

                if (_entries.TryGetValue(LockMetaKey(full), out var meta) && meta.IsExpired)
                {
                    _locks.TryRemove(full, out _);
                    _entries.TryRemove(LockMetaKey(full), out _);
                    continue;
                }

                return Task.FromResult(false);
            }

            if (_locks.TryAdd(full, ownerId))
            {
                TouchLock(full, ownerId, expiry);
                return Task.FromResult(true);
            }
        }
    }

    /// <inheritdoc />
    public Task ReleaseAsync(string resourceKey, string ownerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var full = Prefixed(resourceKey);
        if (_locks.TryGetValue(full, out var existing) && existing == ownerId)
        {
            _locks.TryRemove(full, out _);
            _entries.TryRemove(LockMetaKey(full), out _);
        }

        return Task.CompletedTask;
    }

    internal string Prefixed(string key) =>
        key.StartsWith(_options.KeyPrefix, StringComparison.Ordinal) ? key : _options.KeyPrefix + key;

    private void TouchLock(string full, string ownerId, DateTimeOffset? expiry)
    {
        _locks[full] = ownerId;
        _entries[LockMetaKey(full)] = new Entry(ownerId, expiry);
    }

    private static string LockMetaKey(string full) => full + ":__lockmeta";

    private DateTimeOffset? ResolveExpiry(TimeSpan? ttl)
    {
        if (ttl is { } t) return DateTimeOffset.UtcNow.Add(t);
        if (_options.DefaultTtlSeconds > 0)
            return DateTimeOffset.UtcNow.AddSeconds(_options.DefaultTtlSeconds);
        return null;
    }

    private sealed class Entry(object value, DateTimeOffset? expiresAt)
    {
        public object Value { get; } = value;
        public bool IsExpired => expiresAt is { } e && e <= DateTimeOffset.UtcNow;
    }
}
