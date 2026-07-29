namespace Seven.Application.Interfaces;

/// <summary>Device 服务接口（代码生成）</summary>
public interface ISysDeviceService
{
    Task<Seven.Domain.Common.PageGridData<Seven.Domain.Entities.Business.Device>> GetPageDataAsync(
        Seven.Domain.Common.PageDataOptions options, CancellationToken cancellationToken = default);

    Task<Seven.Domain.Common.WebResponseContent> AddAsync(
        Seven.Domain.Entities.Business.Device entity, CancellationToken cancellationToken = default);

    Task<Seven.Domain.Common.WebResponseContent> UpdateAsync(
        Seven.Domain.Entities.Business.Device entity, CancellationToken cancellationToken = default);

    Task<Seven.Domain.Common.WebResponseContent> DeleteAsync(
        int[] ids, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAsync(
        Seven.Domain.Common.PageDataOptions options, CancellationToken cancellationToken = default);

    byte[] ExportTemplate();

    Task<Seven.Domain.Common.WebResponseContent> ImportAsync(
        Stream stream, CancellationToken cancellationToken = default);
}
