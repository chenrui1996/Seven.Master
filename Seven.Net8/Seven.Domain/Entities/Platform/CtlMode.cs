using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Platform;

/// <summary>联锁/运行模式（全局或按 WCS 包）。</summary>
public class CtlMode : BaseEntity
{
    public int Id { get; set; }
    /// <summary>Global 或 PackId（如 stacker）。</summary>
    public string Scope { get; set; } = string.Empty;
    public WcsControlMode Mode { get; set; } = WcsControlMode.Auto;
    public bool EStop { get; set; }
    public DateTime UpdatedAt { get; set; }
}
