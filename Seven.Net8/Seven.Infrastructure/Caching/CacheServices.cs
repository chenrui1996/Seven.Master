using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Caching;

/// <summary>
/// 内存缓存实现
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    /// <summary>构造函数</summary>
    public MemoryCacheService(IMemoryCache cache) => _cache = cache;

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue) options.AbsoluteExpirationRelativeToNow = expiry;
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveWithDelayedDoubleDeleteAsync(string key, int delayMs = 500, CancellationToken cancellationToken = default)
    {
        await RemoveAsync(key, cancellationToken);
        _ = Task.Run(async () =>
        {
            await Task.Delay(delayMs, cancellationToken);
            await RemoveAsync(key, cancellationToken);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cache.TryGetValue(key, out _));
}

/// <summary>
/// Redis 分布式缓存实现
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    /// <summary>构造函数</summary>
    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(key, cancellationToken);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue) options.AbsoluteExpirationRelativeToNow = expiry;
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(key, cancellationToken);

    /// <inheritdoc />
    public async Task RemoveWithDelayedDoubleDeleteAsync(string key, int delayMs = 500, CancellationToken cancellationToken = default)
    {
        await RemoveAsync(key, cancellationToken);
        _ = Task.Run(async () =>
        {
            await Task.Delay(delayMs, cancellationToken);
            await RemoveAsync(key, cancellationToken);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        await GetAsync<string>(key, cancellationToken) != null;
}

/// <summary>
/// 带延迟双删配置的缓存装饰器
/// </summary>
public class DelayedDoubleDeleteCacheService : ICacheService
{
    private readonly ICacheService _inner;
    private readonly int _delayMs;

    /// <summary>构造函数</summary>
    public DelayedDoubleDeleteCacheService(ICacheService inner, IOptions<CacheOptions> options)
    {
        _inner = inner;
        _delayMs = options.Value.DelayedDeleteMs;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        _inner.GetAsync<T>(key, cancellationToken);

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) =>
        _inner.SetAsync(key, value, expiry, cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _inner.RemoveWithDelayedDoubleDeleteAsync(key, _delayMs, cancellationToken);

    /// <inheritdoc />
    public Task RemoveWithDelayedDoubleDeleteAsync(string key, int delayMs = 500, CancellationToken cancellationToken = default) =>
        _inner.RemoveWithDelayedDoubleDeleteAsync(key, delayMs, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        _inner.ExistsAsync(key, cancellationToken);
}
