using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>四向停车账本：预订 / 占用 / 释放（派车依赖空闲位）。</summary>
public class FwParkingLedger : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? LocationCode { get; set; }
    /// <summary>所属层（MaxShuttle 按层+巷计数；预订时可从货位回填）。</summary>
    public string? LayerCode { get; set; }
    /// <summary>所属巷（MaxShuttle 按层+巷计数）。</summary>
    public string? AisleCode { get; set; }
    public FwParkingStatus Status { get; set; }
    /// <summary>占用方（通常为 RetrievalTask.Id）。</summary>
    public Guid? OwnerId { get; set; }
    public string? ContainerCode { get; set; }
    public string? ShuttleNo { get; set; }
}
