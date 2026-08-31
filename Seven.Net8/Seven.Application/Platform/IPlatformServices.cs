using Seven.Domain.Common;
using Seven.Domain.Entities.Platform;
using Seven.Domain.Enums;

namespace Seven.Application.Platform;

public interface IInterfaceLogService
{
    Task<long> WriteAsync(InterfaceLogWriteRequest request, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<PageGridData<IfcApiLog>> GetPageDataAsync(PageDataOptions options, CancellationToken ct = default);
}

public interface IControlModeService
{
    Task<ControlModeState> GetAsync(string scope, CancellationToken ct = default);
    Task SetModeAsync(string scope, WcsControlMode mode, CancellationToken ct = default);
    Task SetEStopAsync(string scope, bool eStop, CancellationToken ct = default);
    Task<bool> CanAcceptLegsAsync(string packId, CancellationToken ct = default);
}
