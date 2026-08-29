using Seven.Domain.Common;
using Seven.Domain.Entities.Platform;

namespace Seven.Application.Scada;

public interface IScadaViewService
{
    Task<ScdView?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<ScdView?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PageGridData<ScdView>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WebResponseContent> AddAsync(ScdView entity, CancellationToken ct = default);
    Task<WebResponseContent> UpdateAsync(ScdView entity, CancellationToken ct = default);
    Task<ScdViewStatusDto?> GetStatusAsync(int viewId, CancellationToken ct = default);
}

public interface IScadaNodeBindService
{
    Task<PageGridData<ScdNodeBind>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<ScdNodeBind>> ListByViewIdAsync(int viewId, CancellationToken ct = default);
    Task<WebResponseContent> AddAsync(ScdNodeBind entity, CancellationToken ct = default);
    Task<WebResponseContent> UpdateAsync(ScdNodeBind entity, CancellationToken ct = default);
}
