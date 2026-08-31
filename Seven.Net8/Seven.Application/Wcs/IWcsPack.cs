namespace Seven.Application.Wcs;

/// <summary>WCS 包契约：接单、取消、查询与健康检查。</summary>
public interface IWcsPack
{
    string PackId { get; }
    Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default);
    Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default);
    Task CancelLegAsync(Guid legId, CancellationToken ct = default);
    Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default);
    Task<PackHealthDto> HealthAsync(CancellationToken ct = default);
}
