using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>堆垛货位扩展（双深组、LockBin；挂靠 Wms_Location.Code）。</summary>
public class StkLocationProfile : BaseEntity
{
    public int Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    /// <summary>双深组编码；同组浅/深位共享。</summary>
    public string? BinGroupCode { get; set; }
    /// <summary>入库侧 LockBin（不可再入）。</summary>
    public bool InLockBin { get; set; }
    /// <summary>出库侧 LockBin（不可再出）。</summary>
    public bool OutLockBin { get; set; }
}
