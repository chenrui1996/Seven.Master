using System.Text.Json;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;
using StackExchange.Redis;

namespace Seven.Infrastructure.HotStore;

/// <summary>Redis 热存储（JSON 值 + Lua 占用）</summary>
public sealed class RedisHotStore : IHotStore, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly LuaScript AcquireScript = LuaScript.Prepare("""
        local cur = redis.call('GET', @key)
        if (not cur) or cur == @owner then
          if tonumber(@ttlMs) > 0 then
            redis.call('SET', @key, @owner, 'PX', @ttlMs)
          else
            redis.call('SET', @key, @owner)
          end
          return 1
        end
        return 0
        """);

    private static readonly LuaScript ReleaseScript = LuaScript.Prepare("""
        if redis.call('GET', @key) == @owner then
          return redis.call('DEL', @key)
        end
        return 0
        """);

    private readonly IConnectionMultiplexer _mux;
    private readonly HotStoreOptions _options;
    private volatile bool _ready;
    private readonly bool _ownsMux;

    public RedisHotStore(IConnectionMultiplexer mux, IOptions<HotStoreOptions> options, bool ownsMux = false)
    {
        _mux = mux;
        _options = options.Value;
        _ownsMux = ownsMux;
    }

    /// <inheritdoc />
    public bool IsReady => _ready;

    /// <summary>预热完成后由宿主标记就绪</summary>
    public void MarkReady() => _ready = true;

    /// <inheritdoc />
    public Task WarmupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = _mux.GetDatabase();
        var json = await db.StringGetAsync(Prefixed(key)).ConfigureAwait(false);
        if (json.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>((string)json!, JsonOptions);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var db = _mux.GetDatabase();
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var expiry = ResolveTtl(ttl);
        if (expiry is { } e)
            await db.StringSetAsync(Prefixed(key), json, e).ConfigureAwait(false);
        else
            await db.StringSetAsync(Prefixed(key), json).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TryUpdateAsync<T>(string key, Func<T?, T> updater, CancellationToken cancellationToken = default)
    {
        var current = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        var next = updater(current);
        await SetAsync(key, next, null, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _mux.GetDatabase().KeyDeleteAsync(Prefixed(key));

    /// <inheritdoc />
    public async Task<bool> TryAcquireAsync(string resourceKey, string ownerId, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var db = _mux.GetDatabase();
        var ttlMs = (long)(ResolveTtl(ttl)?.TotalMilliseconds ?? 0);
        var result = await db.ScriptEvaluateAsync(AcquireScript, new
        {
            key = (RedisKey)Prefixed(resourceKey),
            owner = (RedisValue)ownerId,
            ttlMs
        }).ConfigureAwait(false);
        return (int)result == 1;
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(string resourceKey, string ownerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var db = _mux.GetDatabase();
        await db.ScriptEvaluateAsync(ReleaseScript, new
        {
            key = (RedisKey)Prefixed(resourceKey),
            owner = (RedisValue)ownerId
        }).ConfigureAwait(false);
    }

    internal string Prefixed(string key) =>
        key.StartsWith(_options.KeyPrefix, StringComparison.Ordinal) ? key : _options.KeyPrefix + key;

    private TimeSpan? ResolveTtl(TimeSpan? ttl)
    {
        if (ttl is { } t) return t;
        if (_options.DefaultTtlSeconds > 0) return TimeSpan.FromSeconds(_options.DefaultTtlSeconds);
        return null;
    }

    public void Dispose()
    {
        if (_ownsMux) _mux.Dispose();
    }
}
