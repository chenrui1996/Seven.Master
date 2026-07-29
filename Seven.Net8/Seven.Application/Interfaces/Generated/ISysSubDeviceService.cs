namespace Seven.Application.Interfaces;

/// <summary>SubDevice 服务接口（Device 子表 Demo）</summary>
public interface ISysSubDeviceService
{
    Task<Seven.Domain.Common.PageGridData<Seven.Domain.Entities.Board.SubDevice>> GetPageDataAsync(
        Seven.Domain.Common.PageDataOptions options, CancellationToken cancellationToken = default);

    Task<Seven.Domain.Common.WebResponseContent> AddAsync(
        Seven.Domain.Entities.Board.SubDevice entity, CancellationToken cancellationToken = default);

    Task<Seven.Domain.Common.WebResponseContent> UpdateAsync(
        Seven.Domain.Entities.Board.SubDevice entity, CancellationToken cancellationToken = default);

    Task<Seven.Domain.Common.WebResponseContent> DeleteAsync(
        int[] ids, CancellationToken cancellationToken = default);

    Task<byte[]> ExportAsync(
        Seven.Domain.Common.PageDataOptions options, CancellationToken cancellationToken = default);

    byte[] ExportTemplate();

    Task<Seven.Domain.Common.WebResponseContent> ImportAsync(
        Stream stream, CancellationToken cancellationToken = default);
}
