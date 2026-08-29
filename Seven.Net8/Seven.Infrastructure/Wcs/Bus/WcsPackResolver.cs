using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs.Bus;

/// <summary>按 PackId 或 CanHandle 解析已注册包。</summary>
public sealed class WcsPackResolver : IWcsPackResolver
{
    private readonly IReadOnlyList<IWcsPack> _packs;

    public WcsPackResolver(IEnumerable<IWcsPack> packs)
        => _packs = packs.ToList();

    public IReadOnlyList<IWcsPack> GetPacks() => _packs;

    public IWcsPack? ResolveByPackId(string packId)
        => _packs.FirstOrDefault(p => string.Equals(p.PackId, packId, StringComparison.OrdinalIgnoreCase));

    public async Task<IWcsPack?> ResolveByCanHandleAsync(
        string fromLocationCode,
        string toLocationCode,
        CancellationToken ct = default)
    {
        foreach (var pack in _packs)
        {
            if (await pack.CanHandleAsync(fromLocationCode, toLocationCode, ct).ConfigureAwait(false))
                return pack;
        }

        return null;
    }
}
