using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Seven.Application.Interfaces;
using Seven.Infrastructure.Configuration;

namespace Seven.Infrastructure.Storage;

/// <summary>MinIO 对象存储</summary>
public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _client;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(IOptions<MinioOptions> options, ILogger<MinioFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);
        var objectName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var size = stream.CanSeek ? stream.Length : -1;
        if (!stream.CanSeek)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectName)
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType),
                cancellationToken);
        }
        else
        {
            await _client.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(size)
                .WithContentType(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType),
                cancellationToken);
        }

        return await GetUrlAsync(objectName, cancellationToken);
    }

    public Task<string> GetUrlAsync(string objectName, CancellationToken cancellationToken = default)
    {
        var scheme = _options.UseSsl ? "https" : "http";
        return Task.FromResult($"{scheme}://{_options.Endpoint}/{_options.Bucket}/{objectName.TrimStart('/')}");
    }

    async Task EnsureBucketAsync(CancellationToken ct)
    {
        try
        {
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_options.Bucket), ct);
            if (!exists)
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_options.Bucket), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "确保 MinIO Bucket 失败");
            throw;
        }
    }
}
