using Microsoft.EntityFrameworkCore;
using Seven.Application.Interfaces;
using Seven.Domain.Common;
using Seven.Domain.Entities.Board;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Board;

/// <summary>
/// 设备/大屏服务
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly SevenDbContext _db;

    /// <summary>构造函数</summary>
    public DeviceService(SevenDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<PageGridData<Device>> GetPageDataAsync(PageDataOptions options, CancellationToken cancellationToken = default)
    {
        var query = _db.Devices.AsNoTracking().OrderBy(d => d.DeviceId);
        return await CrudHelper.PaginateAsync(query, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WebResponseContent> GetChartDataAsync(CancellationToken cancellationToken = default)
    {
        var devices = await _db.Devices.AsNoTracking().ToListAsync(cancellationToken);
        var chartData = new
        {
            total = devices.Count,
            online = devices.Count(d => d.Status == 1),
            offline = devices.Count(d => d.Status == 0),
            categories = devices.GroupBy(d => d.Location).Select(g => new { name = g.Key ?? "未知", value = g.Count() })
        };
        return WebResponseContent.Ok(data: chartData);
    }
}
