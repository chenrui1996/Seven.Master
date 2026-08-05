using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.DeviceComm;

/// <summary>设备通讯连接配置</summary>
public class CommConnection : BaseEntity
{
    public int CommConnectionId { get; set; }

    /// <summary>显示名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>协议</summary>
    public CommProtocol Protocol { get; set; } = CommProtocol.Step7;

    /// <summary>主机 IP / 主机名</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>端口（Step7 默认 102，Modbus TCP 默认 502）</summary>
    public int Port { get; set; } = 102;

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>服务启动后自动连接</summary>
    public bool AutoConnect { get; set; } = true;

    /// <summary>可选关联业务设备 Id</summary>
    public int? BizDeviceId { get; set; }

    /// <summary>Step7 Rack</summary>
    public short Rack { get; set; }

    /// <summary>Step7 Slot</summary>
    public short Slot { get; set; } = 1;

    /// <summary>Step7 Cpu 类型字符串，如 S71200 / S71500 / S7300</summary>
    public string? CpuType { get; set; } = "S71200";

    /// <summary>Modbus 从站 UnitId</summary>
    public byte UnitId { get; set; } = 1;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>通讯点位</summary>
public class CommPoint : BaseEntity
{
    public int CommPointId { get; set; }

    public int CommConnectionId { get; set; }

    /// <summary>点位编码（规则中引用）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>显示名称</summary>
    public string Name { get; set; } = string.Empty;

    public CommDataType DataType { get; set; } = CommDataType.Int16;

    /// <summary>统一地址字符串（Step7: DB1.DBW0；Modbus: HR:0）</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Step7 DB 号</summary>
    public int? DbNumber { get; set; }

    /// <summary>字节偏移</summary>
    public int? ByteOffset { get; set; }

    /// <summary>位偏移（Bool）</summary>
    public int? BitOffset { get; set; }

    /// <summary>Modbus 区</summary>
    public CommModbusArea? ModbusArea { get; set; }

    /// <summary>Modbus 起始地址</summary>
    public int? ModbusAddress { get; set; }

    /// <summary>字符串长度或寄存器数量</summary>
    public int Quantity { get; set; } = 1;

    public string? Remark { get; set; }
}

/// <summary>组合事件规则</summary>
public class CommRule : BaseEntity
{
    public int CommRuleId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>绑定连接；空表示跨连接（通过点位 Code 解析）</summary>
    public int? CommConnectionId { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>扫描周期覆盖（毫秒）；空用全局</summary>
    public int? ScanIntervalMs { get; set; }

    /// <summary>规则定义 JSON</summary>
    public string DefinitionJson { get; set; } = "{}";

    /// <summary>事件名（Emit 默认）</summary>
    public string? EventName { get; set; }

    public string? Remark { get; set; }
}

/// <summary>规则触发日志</summary>
public class CommEventLog : BaseEntity
{
    public long CommEventLogId { get; set; }

    public int CommRuleId { get; set; }

    public string? EventName { get; set; }

    public bool Success { get; set; }

    /// <summary>快照 JSON</summary>
    public string? PayloadJson { get; set; }

    public string? Message { get; set; }
}
