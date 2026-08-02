using FluentAssertions;
using Microsoft.Extensions.Options;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.HotStore;
using System.Threading.Channels;

namespace Seven.Tests.Unit;

public class HotStoreTests
{
    private static MemoryHotStore CreateMemory(string prefix = "hot:") =>
        new(Options.Create(new HotStoreOptions { KeyPrefix = prefix, DefaultTtlSeconds = 0 }));

    [Fact]
    public async Task Memory_SetGetRemove_Works()
    {
        var store = CreateMemory();
        store.MarkReady();

        await store.SetAsync("path:1", "A-B");
        (await store.GetAsync<string>("path:1")).Should().Be("A-B");

        await store.RemoveAsync("path:1");
        (await store.GetAsync<string>("path:1")).Should().BeNull();
    }

    [Fact]
    public async Task Memory_TryAcquire_IsExclusive()
    {
        var store = CreateMemory();
        (await store.TryAcquireAsync("lock:N1", "V1")).Should().BeTrue();
        (await store.TryAcquireAsync("lock:N1", "V2")).Should().BeFalse();
        (await store.TryAcquireAsync("lock:N1", "V1")).Should().BeTrue();

        await store.ReleaseAsync("lock:N1", "V1");
        (await store.TryAcquireAsync("lock:N1", "V2")).Should().BeTrue();
    }

    [Fact]
    public async Task Memory_TryUpdate_IsSerializedPerKey()
    {
        var store = CreateMemory();
        await store.SetAsync("counter", 0);
        await store.TryUpdateAsync<int>("counter", c => (c) + 1);
        await store.TryUpdateAsync<int>("counter", c => (c) + 1);
        (await store.GetAsync<int>("counter")).Should().Be(2);
    }

    [Fact]
    public async Task Tracking_PublishesDirtyChanges()
    {
        var channel = Channel.CreateUnbounded<HotStoreChange>();
        var inner = CreateMemory();
        var tracking = new TrackingHotStore(inner, channel.Writer);
        tracking.MarkReady();

        await tracking.SetAsync("wcs:node:N1", "busy");
        (await tracking.TryAcquireAsync("wcs:lock:N1", "V1")).Should().BeTrue();
        await tracking.ReleaseAsync("wcs:lock:N1", "V1");

        var changes = new List<HotStoreChange>();
        while (channel.Reader.TryRead(out var c)) changes.Add(c);

        changes.Should().Contain(c => c.Kind == HotStoreChangeKind.Set && c.Key == "wcs:node:N1");
        changes.Should().Contain(c => c.Kind == HotStoreChangeKind.Acquire && c.OwnerId == "V1");
        changes.Should().Contain(c => c.Kind == HotStoreChangeKind.Release && c.OwnerId == "V1");
    }

    [Fact]
    public async Task DisabledHotStore_ThrowsOnWrite()
    {
        var store = new DisabledHotStore();
        store.IsReady.Should().BeFalse();
        var act = async () => await store.SetAsync("k", 1);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
