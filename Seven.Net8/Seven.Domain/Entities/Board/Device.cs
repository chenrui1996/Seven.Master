using Seven.Domain.Attributes;
using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Board;

/// <summary>
/// 设备信息（大屏模块）
/// </summary>
public class Device : BaseEntity
{
    /// <summary>设备 Id</summary>
    public int DeviceId { get; set; }

    /// <summary>设备名称</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>设备编码</summary>
    public string? DeviceCode { get; set; }

    /// <summary>设备状态</summary>
    [FormEnum(typeof(DeviceStatus))]
    public int Status { get; set; }

    /// <summary>位置</summary>
    public string? Location { get; set; }
}
