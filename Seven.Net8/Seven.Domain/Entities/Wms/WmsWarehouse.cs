using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wms;

/// <summary>仓库主数据</summary>
public class WmsWarehouse : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>启用的 WCS 包，逗号分隔，如 stacker,fourway。至少一种。</summary>
    public string EnabledPackIds { get; set; } = string.Empty;

    /// <summary>盘点锁：为 true 时堆垛分配跳过本仓巷道（对齐 LES Warehouse.LockFlag）。</summary>
    public bool IsCycleCountLocked { get; set; }
}
