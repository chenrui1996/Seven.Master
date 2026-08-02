namespace Seven.Application.Interfaces;

/// <summary>热数据变更类型</summary>
public enum HotStoreChangeKind
{
    /// <summary>写入或更新值</summary>
    Set = 0,

    /// <summary>删除键</summary>
    Remove = 1,

    /// <summary>占用资源成功</summary>
    Acquire = 2,

    /// <summary>释放资源</summary>
    Release = 3
}

/// <summary>热层脏数据变更（供异步落库）</summary>
public sealed record HotStoreChange(
    string Key,
    HotStoreChangeKind Kind,
    object? Value,
    string? OwnerId,
    DateTimeOffset Timestamp);

/// <summary>
/// 运营热数据通道：热层为真相源，不走 cache-aside miss 回库，无延迟双删。
/// 与 <see cref="ICacheService"/> 职责分离。
/// </summary>
public interface IHotStore
{
    /// <summary>预热是否完成，调度循环应在就绪后运行</summary>
    bool IsReady { get; }

    /// <summary>执行全部 IHotStoreWarmup（通常由宿主调用）</summary>
    Task WarmupAsync(CancellationToken cancellationToken = default);

    /// <summary>读取热数据；未命中返回 default，不回源数据库</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>写入热数据并标记脏数据</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>读取-修改-写入（Memory 按 key 互斥；Redis 为 get+set）</summary>
    Task<bool> TryUpdateAsync<T>(string key, Func<T?, T> updater, CancellationToken cancellationToken = default);

    /// <summary>删除热数据并标记脏数据</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>原子占用资源（节点/边等）；已占用且 owner 不同则失败</summary>
    Task<bool> TryAcquireAsync(string resourceKey, string ownerId, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>仅当 owner 匹配时释放占用</summary>
    Task ReleaseAsync(string resourceKey, string ownerId, CancellationToken cancellationToken = default);
}

/// <summary>启动预热扩展点：将冷库路网/配置装入热层</summary>
public interface IHotStoreWarmup
{
    /// <summary>预热步骤名称（日志用）</summary>
    string Name { get; }

    /// <summary>执行预热</summary>
    Task WarmupAsync(IHotStore store, CancellationToken cancellationToken = default);
}

/// <summary>异步落库扩展点：批量将脏变更写入数据库</summary>
public interface IHotStorePersister
{
    /// <summary>落库器名称（日志用）</summary>
    string Name { get; }

    /// <summary>处理一批变更；可按 Key 前缀过滤</summary>
    Task PersistBatchAsync(IReadOnlyList<HotStoreChange> changes, CancellationToken cancellationToken = default);
}
