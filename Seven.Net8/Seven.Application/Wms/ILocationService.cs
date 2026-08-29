using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface ILocationService
{
    Task<WmsLocation?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PageGridData<WmsLocation>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WebResponseContent> AddAsync(WmsLocation entity, CancellationToken ct = default);
    Task<WebResponseContent> UpdateAsync(WmsLocation entity, CancellationToken ct = default);
}
