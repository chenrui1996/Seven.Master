using Seven.Application.Interfaces;

namespace Seven.Infrastructure.HotStore;

/// <summary>Features.HotStore=false 时的占位实现</summary>
public sealed class DisabledHotStore : IHotStore
{
    /// <inheritdoc />
    public bool IsReady => false;

    /// <inheritdoc />
    public Task WarmupAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(default(T));

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) =>
        throw Disabled();

    /// <inheritdoc />
    public Task<bool> TryUpdateAsync<T>(string key, Func<T?, T> updater, CancellationToken cancellationToken = default) =>
        throw Disabled();

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        throw Disabled();

    /// <inheritdoc />
    public Task<bool> TryAcquireAsync(string resourceKey, string ownerId, TimeSpan? ttl = null, CancellationToken cancellationToken = default) =>
        throw Disabled();

    /// <inheritdoc />
    public Task ReleaseAsync(string resourceKey, string ownerId, CancellationToken cancellationToken = default) =>
        throw Disabled();

    private static InvalidOperationException Disabled() =>
        new("HotStore is disabled. Set Features:HotStore=true and configure the HotStore section.");
}
