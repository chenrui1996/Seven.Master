using Microsoft.EntityFrameworkCore;
using Seven.Application.Platform;
using Seven.Domain.Common;
using Seven.Domain.Entities.Platform;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Services;

namespace Seven.Infrastructure.Platform;

public sealed class InterfaceLogService : IInterfaceLogService
{
    private readonly SevenDbContext _db;

    public InterfaceLogService(SevenDbContext db) => _db = db;

    public async Task<long> WriteAsync(InterfaceLogWriteRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var row = new IfcApiLog
        {
            Direction = request.Direction,
            SystemCode = request.SystemCode,
            CorrelationId = request.CorrelationId,
            LegId = request.LegId,
            OrderNo = request.OrderNo,
            Path = request.Path,
            RequestBody = request.RequestBody,
            ResponseBody = request.ResponseBody,
            DurationMs = request.DurationMs,
            Success = request.Success,
            ErrorMessage = request.ErrorMessage,
            CreateDate = DateTime.UtcNow
        };
        _db.IfcApiLogs.Add(row);
        await _db.SaveChangesAsync(ct);
        return row.Id;
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.IfcApiLogs.CountAsync(ct);

    public Task<PageGridData<IfcApiLog>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default) =>
        CrudHelper.PaginateAsync(_db.IfcApiLogs.AsNoTracking(), options, ct);
}

public sealed class ControlModeService : IControlModeService
{
    private readonly SevenDbContext _db;

    public ControlModeService(SevenDbContext db) => _db = db;

    public async Task<ControlModeState> GetAsync(string scope, CancellationToken ct = default)
    {
        var row = await FindRowAsync(scope, ct);
        return row == null
            ? DefaultState(scope)
            : ToState(row);
    }

    public async Task SetModeAsync(string scope, WcsControlMode mode, CancellationToken ct = default)
    {
        var row = await RequireRowAsync(scope, ct);
        row.Mode = mode;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetEStopAsync(string scope, bool eStop, CancellationToken ct = default)
    {
        var row = await RequireRowAsync(scope, ct);
        row.EStop = eStop;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> CanAcceptLegsAsync(string packId, CancellationToken ct = default)
    {
        var global = await GetAsync(ControlScopes.Global, ct);
        if (global.EStop)
            return false;

        var pack = await GetAsync(packId, ct);
        if (pack.EStop)
            return false;

        var effectiveMode = await HasPersistedScopeAsync(packId, ct)
            ? pack.Mode
            : global.Mode;
        return effectiveMode != WcsControlMode.Manual;
    }

    private async Task<bool> HasPersistedScopeAsync(string scope, CancellationToken ct)
        => await _db.CtlModes.AnyAsync(x => x.Scope == scope, ct);

    private async Task<CtlMode?> FindRowAsync(string scope, CancellationToken ct)
        => await _db.CtlModes.FirstOrDefaultAsync(x => x.Scope == scope, ct);

    private async Task<CtlMode> RequireRowAsync(string scope, CancellationToken ct)
    {
        var row = await FindRowAsync(scope, ct);
        if (row != null)
            return row;

        row = new CtlMode
        {
            Scope = scope,
            Mode = WcsControlMode.Auto,
            EStop = false,
            UpdatedAt = DateTime.UtcNow,
            CreateDate = DateTime.UtcNow
        };
        _db.CtlModes.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    private static ControlModeState DefaultState(string scope)
        => new(scope, WcsControlMode.Auto, false, DateTime.UtcNow);

    private static ControlModeState ToState(CtlMode row)
        => new(row.Scope, row.Mode, row.EStop, row.UpdatedAt);
}
