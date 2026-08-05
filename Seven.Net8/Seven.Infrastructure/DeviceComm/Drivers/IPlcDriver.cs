using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Enums;

namespace Seven.Infrastructure.DeviceComm.Drivers;

/// <summary>底层 PLC/设备驱动</summary>
public interface IPlcDriver : IAsyncDisposable
{
    CommProtocol Protocol { get; }
    bool IsConnected { get; }

    Task ConnectAsync(CommConnection connection, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<object?> ReadAsync(CommPoint point, CancellationToken cancellationToken = default);
    Task WriteAsync(CommPoint point, object? value, CancellationToken cancellationToken = default);

    Task<object?> ReadRawAsync(string address, CommDataType dataType, int quantity, CancellationToken cancellationToken = default);
    Task WriteRawAsync(string address, CommDataType dataType, object? value, CancellationToken cancellationToken = default);
}

/// <summary>按协议创建驱动</summary>
public interface IPlcDriverFactory
{
    IPlcDriver Create(CommProtocol protocol);
}
