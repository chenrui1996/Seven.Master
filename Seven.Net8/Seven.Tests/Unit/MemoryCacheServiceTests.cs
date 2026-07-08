using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Seven.Infrastructure.Caching;

namespace Seven.Tests.Unit;

/// <summary>
/// 内存缓存与延迟双删单元测试
/// </summary>
public class MemoryCacheServiceTests
{
    [Fact]
    public async Task SetAndGet_ShouldReturnStoredValue()
    {
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));

        await cache.SetAsync("key1", "value1");
        var result = await cache.GetAsync<string>("key1");

        result.Should().Be("value1");
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        await cache.SetAsync("key2", 42);

        await cache.RemoveAsync("key2");

        (await cache.ExistsAsync("key2")).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveWithDelayedDoubleDelete_ShouldDeleteAfterDelay()
    {
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        await cache.SetAsync("key3", "data");

        await cache.RemoveWithDelayedDoubleDeleteAsync("key3", delayMs: 100);

        (await cache.ExistsAsync("key3")).Should().BeFalse();
        await Task.Delay(150);
        (await cache.ExistsAsync("key3")).Should().BeFalse();
    }
}
