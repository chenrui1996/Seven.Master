using Seven.Application.Interfaces;

namespace Seven.Infrastructure.Storage;

/// <summary>
/// 本地文件存储实现（MinIO 不可用时的兜底方案）
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadPath;

    /// <summary>构造函数</summary>
    public LocalFileStorageService()
    {
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Upload");
        Directory.CreateDirectory(_uploadPath);
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_uploadPath, safeName);
        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, cancellationToken);
        return $"/upload/{safeName}";
    }

    /// <inheritdoc />
    public Task<string> GetUrlAsync(string objectName, CancellationToken cancellationToken = default) =>
        Task.FromResult(objectName.StartsWith('/') ? objectName : $"/upload/{objectName}");
}
