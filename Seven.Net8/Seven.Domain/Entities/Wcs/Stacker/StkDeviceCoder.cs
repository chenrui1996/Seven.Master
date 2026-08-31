using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.Stacker;

/// <summary>设备点 ↔ 货位/逻辑码（JudgeMap：Bin→巷口/Srm 点）。</summary>
public class StkDeviceCoder : BaseEntity
{
    public int Id { get; set; }
    /// <summary>Wms 货位码或业务码。</summary>
    public string LocationCode { get; set; } = string.Empty;
    /// <summary>路网点码（申请点/EP/输送点）。</summary>
    public string PointCode { get; set; } = string.Empty;
}
