using System.Globalization;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Enums;
using S7.Net;

namespace Seven.Infrastructure.DeviceComm.Drivers;

/// <summary>西门子 Step7 驱动（S7netplus）</summary>
public sealed class Step7PlcDriver : IPlcDriver
{
    private Plc? _plc;
    private readonly object _gate = new();

    public CommProtocol Protocol => CommProtocol.Step7;
    public bool IsConnected => _plc?.IsConnected == true;

    public Task ConnectAsync(CommConnection connection, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            DisconnectCore();
            var cpu = ParseCpu(connection.CpuType);
            var port = connection.Port > 0 ? connection.Port : 102;
            _plc = new Plc(cpu, connection.Host, (short)port, connection.Rack, connection.Slot);
            _plc.Open();
            if (!_plc.IsConnected)
                throw new InvalidOperationException($"Step7 连接失败: {connection.Host}:{port}");
        }
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate) DisconnectCore();
        return Task.CompletedTask;
    }

    public Task<object?> ReadAsync(CommPoint point, CancellationToken cancellationToken = default)
    {
        var address = ResolveAddress(point);
        return ReadRawAsync(address, point.DataType, Math.Max(1, point.Quantity), cancellationToken);
    }

    public Task WriteAsync(CommPoint point, object? value, CancellationToken cancellationToken = default)
    {
        var address = ResolveAddress(point);
        return WriteRawAsync(address, point.DataType, value, cancellationToken);
    }

    public Task<object?> ReadRawAsync(string address, CommDataType dataType, int quantity, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            EnsureConnected();
            var result = _plc!.Read(address);
            return Task.FromResult(ConvertRead(result, dataType));
        }
    }

    public Task WriteRawAsync(string address, CommDataType dataType, object? value, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            EnsureConnected();
            _plc!.Write(address, ConvertWrite(value, dataType));
        }
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        DisconnectCore();
        return ValueTask.CompletedTask;
    }

    private void EnsureConnected()
    {
        if (_plc == null || !_plc.IsConnected)
            throw new InvalidOperationException("Step7 未连接");
    }

    private void DisconnectCore()
    {
        try { _plc?.Close(); } catch { /* ignore */ }
        _plc = null;
    }

    public static string ResolveAddress(CommPoint point)
    {
        if (!string.IsNullOrWhiteSpace(point.Address))
            return point.Address.Trim();

        if (point.DbNumber is null || point.ByteOffset is null)
            throw new InvalidOperationException($"点位 {point.Code} 缺少 Address 或 DB/Offset");

        var db = point.DbNumber.Value;
        var off = point.ByteOffset.Value;
        return point.DataType switch
        {
            CommDataType.Bool => $"DB{db}.DBX{off}.{point.BitOffset ?? 0}",
            CommDataType.Byte => $"DB{db}.DBB{off}",
            CommDataType.Int16 or CommDataType.UInt16 => $"DB{db}.DBW{off}",
            CommDataType.Int32 or CommDataType.UInt32 or CommDataType.Float => $"DB{db}.DBD{off}",
            CommDataType.String => $"DB{db}.DBB{off}",
            _ => $"DB{db}.DBW{off}"
        };
    }

    private static CpuType ParseCpu(string? cpuType)
    {
        var s = (cpuType ?? "S71200").Replace("-", "").Replace("_", "").ToUpperInvariant();
        return s switch
        {
            "S7200" => CpuType.S7200,
            "S7300" => CpuType.S7300,
            "S7400" => CpuType.S7400,
            "S71200" => CpuType.S71200,
            "S71500" => CpuType.S71500,
            _ => Enum.TryParse<CpuType>(s, true, out var c) ? c : CpuType.S71200
        };
    }

    private static object? ConvertRead(object? raw, CommDataType dataType)
    {
        if (raw == null) return null;
        return dataType switch
        {
            CommDataType.Bool => Convert.ToBoolean(raw, CultureInfo.InvariantCulture),
            CommDataType.Byte => Convert.ToByte(raw, CultureInfo.InvariantCulture),
            CommDataType.Int16 => Convert.ToInt16(raw, CultureInfo.InvariantCulture),
            CommDataType.UInt16 => Convert.ToUInt16(raw, CultureInfo.InvariantCulture),
            CommDataType.Int32 => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
            CommDataType.UInt32 => Convert.ToUInt32(raw, CultureInfo.InvariantCulture),
            CommDataType.Float => Convert.ToSingle(raw, CultureInfo.InvariantCulture),
            CommDataType.Double => Convert.ToDouble(raw, CultureInfo.InvariantCulture),
            CommDataType.String => Convert.ToString(raw, CultureInfo.InvariantCulture),
            _ => raw
        };
    }

    private static object ConvertWrite(object? value, CommDataType dataType)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value is System.Text.Json.JsonElement je)
            value = JsonElementToObject(je);

        return dataType switch
        {
            CommDataType.Bool => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
            CommDataType.Byte => Convert.ToByte(value, CultureInfo.InvariantCulture),
            CommDataType.Int16 => Convert.ToInt16(value, CultureInfo.InvariantCulture),
            CommDataType.UInt16 => Convert.ToUInt16(value, CultureInfo.InvariantCulture),
            CommDataType.Int32 => Convert.ToInt32(value, CultureInfo.InvariantCulture),
            CommDataType.UInt32 => Convert.ToUInt32(value, CultureInfo.InvariantCulture),
            CommDataType.Float => Convert.ToSingle(value, CultureInfo.InvariantCulture),
            CommDataType.Double => Convert.ToDouble(value, CultureInfo.InvariantCulture),
            CommDataType.String => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value
        };
    }

    private static object? JsonElementToObject(System.Text.Json.JsonElement je) => je.ValueKind switch
    {
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Number when je.TryGetInt64(out var l) => l,
        System.Text.Json.JsonValueKind.Number => je.GetDouble(),
        System.Text.Json.JsonValueKind.String => je.GetString(),
        _ => je.ToString()
    };
}
