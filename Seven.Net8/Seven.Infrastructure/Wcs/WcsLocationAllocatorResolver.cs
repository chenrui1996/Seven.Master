using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs;

public sealed class WcsLocationAllocatorResolver : IWcsLocationAllocatorResolver
{
    private readonly IReadOnlyDictionary<string, IWcsLocationSchema> _schemas;
    private readonly IReadOnlyDictionary<string, IWcsLocationAllocator> _allocators;

    public WcsLocationAllocatorResolver(
        IEnumerable<IWcsLocationSchema> schemas,
        IEnumerable<IWcsLocationAllocator> allocators)
    {
        _schemas = schemas.ToDictionary(x => x.PackId, StringComparer.OrdinalIgnoreCase);
        _allocators = allocators.ToDictionary(x => x.PackId, StringComparer.OrdinalIgnoreCase);
    }

    public IWcsLocationSchema GetSchema(string packId)
    {
        var key = (packId ?? string.Empty).Trim();
        if (_schemas.TryGetValue(key, out var schema))
            return schema;
        throw new InvalidOperationException($"未注册 LocationSchema: {packId}");
    }

    public IWcsLocationAllocator GetAllocator(string packId)
    {
        var key = (packId ?? string.Empty).Trim();
        if (_allocators.TryGetValue(key, out var allocator))
            return allocator;
        throw new InvalidOperationException($"未注册 LocationAllocator: {packId}");
    }
}
