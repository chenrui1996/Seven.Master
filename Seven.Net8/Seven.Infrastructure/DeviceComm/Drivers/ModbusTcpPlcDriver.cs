using System.Globalization;
using System.Net.Sockets;
using NModbus;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Enums;

namespace Seven.Infrastructure.DeviceComm.Drivers;

/// <summary>Modbus TCP 驱动（NModbus）</summary>
public sealed class ModbusTcpPlcDriver : IPlcDriver
{
    private TcpClient? _client;
    private IModbusMaster? _master;
    private byte _unitId = 1;
    private readonly object _gate = new();

    public CommProtocol Protocol => CommProtocol.ModbusTcp;
    public bool IsConnected => _client?.Connected == true && _master != null;

    public async Task ConnectAsync(CommConnection connection, CancellationToken cancellationToken = default)
    {
        DisconnectCore();
        var port = connection.Port > 0 ? connection.Port : 502;
        _unitId = connection.UnitId;
        var client = new TcpClient();
        await client.ConnectAsync(connection.Host, port, cancellationToken);
        lock (_gate)
        {
            _client = client;
            var factory = new ModbusFactory();
            _master = factory.CreateMaster(_client);
            _master.Transport.ReadTimeout = 3000;
            _master.Transport.WriteTimeout = 3000;
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate) DisconnectCore();
        return Task.CompletedTask;
    }

    public Task<object?> ReadAsync(CommPoint point, CancellationToken cancellationToken = default)
    {
        var (area, address, qty) = Resolve(point);
        return ReadInternalAsync(area, address, qty, point.DataType, cancellationToken);
    }

    public Task WriteAsync(CommPoint point, object? value, CancellationToken cancellationToken = default)
    {
        var (area, address, _) = Resolve(point);
        return WriteInternalAsync(area, address, point.DataType, value, cancellationToken);
    }

    public Task<object?> ReadRawAsync(string address, CommDataType dataType, int quantity, CancellationToken cancellationToken = default)
    {
        var (area, addr, qty) = ParseAddress(address, quantity);
        return ReadInternalAsync(area, addr, qty, dataType, cancellationToken);
    }

    public Task WriteRawAsync(string address, CommDataType dataType, object? value, CancellationToken cancellationToken = default)
    {
        var (area, addr, _) = ParseAddress(address, 1);
        return WriteInternalAsync(area, addr, dataType, value, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        DisconnectCore();
        return ValueTask.CompletedTask;
    }

    private Task<object?> ReadInternalAsync(CommModbusArea area, ushort address, ushort quantity, CommDataType dataType, CancellationToken ct)
    {
        lock (_gate)
        {
            EnsureConnected();
            object? result = area switch
            {
                CommModbusArea.Coil => _master!.ReadCoils(_unitId, address, quantity)[0],
                CommModbusArea.DiscreteInput => _master!.ReadInputs(_unitId, address, quantity)[0],
                CommModbusArea.InputRegister => DecodeRegisters(_master!.ReadInputRegisters(_unitId, address, Math.Max(quantity, RegCount(dataType))), dataType),
                _ => DecodeRegisters(_master!.ReadHoldingRegisters(_unitId, address, Math.Max(quantity, RegCount(dataType))), dataType)
            };
            return Task.FromResult(result);
        }
    }

    private Task WriteInternalAsync(CommModbusArea area, ushort address, CommDataType dataType, object? value, CancellationToken ct)
    {
        lock (_gate)
        {
            EnsureConnected();
            value = Normalize(value);
            switch (area)
            {
                case CommModbusArea.Coil:
                    _master!.WriteSingleCoil(_unitId, address, Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                    break;
                case CommModbusArea.HoldingRegister:
                    var regs = EncodeRegisters(value, dataType);
                    if (regs.Length == 1)
                        _master!.WriteSingleRegister(_unitId, address, regs[0]);
                    else
                        _master!.WriteMultipleRegisters(_unitId, address, regs);
                    break;
                default:
                    throw new InvalidOperationException($"Modbus 区 {area} 不支持写入");
            }
        }
        return Task.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (!IsConnected) throw new InvalidOperationException("Modbus TCP 未连接");
    }

    private void DisconnectCore()
    {
        try { _master?.Dispose(); } catch { /* ignore */ }
        try { _client?.Close(); } catch { /* ignore */ }
        _master = null;
        _client = null;
    }

    public static (CommModbusArea Area, ushort Address, ushort Quantity) Resolve(CommPoint point)
    {
        if (point.ModbusArea is not null && point.ModbusAddress is not null)
            return (point.ModbusArea.Value, (ushort)point.ModbusAddress.Value, (ushort)Math.Max(1, point.Quantity));

        if (!string.IsNullOrWhiteSpace(point.Address))
            return ParseAddress(point.Address, Math.Max(1, point.Quantity));

        throw new InvalidOperationException($"点位 {point.Code} 缺少 Modbus 地址");
    }

    /// <summary>地址格式：HR:0 / Coil:1 / IR:10 / DI:2</summary>
    public static (CommModbusArea Area, ushort Address, ushort Quantity) ParseAddress(string address, int quantity)
    {
        var parts = address.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || !ushort.TryParse(parts[1], out var addr))
            throw new InvalidOperationException($"无效 Modbus 地址: {address}，期望如 HR:0");

        var area = parts[0].ToUpperInvariant() switch
        {
            "C" or "COIL" or "COILS" => CommModbusArea.Coil,
            "DI" or "DISCRETE" or "DISCRETEINPUT" => CommModbusArea.DiscreteInput,
            "IR" or "INPUT" or "INPUTREGISTER" => CommModbusArea.InputRegister,
            "HR" or "HOLDING" or "HOLDINGREGISTER" or "R" => CommModbusArea.HoldingRegister,
            _ => throw new InvalidOperationException($"未知 Modbus 区: {parts[0]}")
        };
        return (area, addr, (ushort)Math.Max(1, quantity));
    }

    private static ushort RegCount(CommDataType dataType) => dataType switch
    {
        CommDataType.Int32 or CommDataType.UInt32 or CommDataType.Float => 2,
        CommDataType.Double => 4,
        _ => 1
    };

    private static object DecodeRegisters(ushort[] regs, CommDataType dataType)
    {
        if (regs.Length == 0) return 0;
        return dataType switch
        {
            CommDataType.Bool => regs[0] != 0,
            CommDataType.Byte => (byte)(regs[0] & 0xFF),
            CommDataType.Int16 => unchecked((short)regs[0]),
            CommDataType.UInt16 => regs[0],
            CommDataType.Int32 when regs.Length >= 2 => (int)(((uint)regs[0] << 16) | regs[1]),
            CommDataType.UInt32 when regs.Length >= 2 => ((uint)regs[0] << 16) | regs[1],
            CommDataType.Float when regs.Length >= 2 => DecodeFloat(regs[0], regs[1]),
            _ => regs[0]
        };
    }

    private static float DecodeFloat(ushort hi, ushort lo)
    {
        var bytes = new byte[]
        {
            (byte)(lo & 0xFF), (byte)(lo >> 8),
            (byte)(hi & 0xFF), (byte)(hi >> 8)
        };
        if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

    private static ushort[] EncodeRegisters(object? value, CommDataType dataType)
    {
        return dataType switch
        {
            CommDataType.Bool => [(ushort)(Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? 1 : 0)],
            CommDataType.Byte or CommDataType.Int16 or CommDataType.UInt16 =>
                [(ushort)Convert.ToUInt16(value, CultureInfo.InvariantCulture)],
            CommDataType.Int32 => EncodeInt32(Convert.ToInt32(value, CultureInfo.InvariantCulture)),
            CommDataType.UInt32 => EncodeUInt32(Convert.ToUInt32(value, CultureInfo.InvariantCulture)),
            CommDataType.Float => EncodeFloat(Convert.ToSingle(value, CultureInfo.InvariantCulture)),
            _ => [(ushort)Convert.ToUInt16(value, CultureInfo.InvariantCulture)]
        };
    }

    private static ushort[] EncodeInt32(int v) => [(ushort)(v >> 16), (ushort)(v & 0xFFFF)];
    private static ushort[] EncodeUInt32(uint v) => [(ushort)(v >> 16), (ushort)(v & 0xFFFF)];
    private static ushort[] EncodeFloat(float v)
    {
        var bytes = BitConverter.GetBytes(v);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return [(ushort)((bytes[0] << 8) | bytes[1]), (ushort)((bytes[2] << 8) | bytes[3])];
    }

    private static object? Normalize(object? value)
    {
        if (value is System.Text.Json.JsonElement je)
        {
            return je.ValueKind switch
            {
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Number when je.TryGetInt64(out var l) => l,
                System.Text.Json.JsonValueKind.Number => je.GetDouble(),
                System.Text.Json.JsonValueKind.String => je.GetString(),
                _ => je.ToString()
            };
        }
        return value;
    }
}
