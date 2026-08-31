using Seven.Domain.Common;
using Seven.Domain.Entities.Wms;

namespace Seven.Application.Wms;

public interface IContainerService
{
    Task<WmsContainer?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PageGridData<WmsContainer>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
    Task<WebResponseContent> AddAsync(WmsContainer entity, CancellationToken ct = default);
    Task<WebResponseContent> UpdateAsync(WmsContainer entity, CancellationToken ct = default);
}
