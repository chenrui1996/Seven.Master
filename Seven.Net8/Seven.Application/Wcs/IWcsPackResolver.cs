namespace Seven.Application.Wcs;

/// <summary>按 PackId 或 CanHandle 解析已注册 WCS 包。</summary>
public interface IWcsPackResolver
{
    IReadOnlyList<IWcsPack> GetPacks();
    IWcsPack? ResolveByPackId(string packId);
    Task<IWcsPack?> ResolveByCanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default);
}
