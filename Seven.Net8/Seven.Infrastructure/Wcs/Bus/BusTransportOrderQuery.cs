using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Common;
using Seven.Domain.Entities.Bus;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Wcs.Bus;

public sealed class BusTransportOrderQuery : IBusTransportOrderQuery
{
    private readonly SevenDbContext _db;

    public BusTransportOrderQuery(SevenDbContext db) => _db = db;

    public Task<PageGridData<BusTransportOrder>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.BusTransportOrders.AsNoTracking(), options, ct);
}
