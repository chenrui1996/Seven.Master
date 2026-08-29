namespace Seven.Domain.Enums;

/// <summary>容器状态</summary>
public enum WmsContainerStatus
{
    Empty = 0,
    Occupied = 1,
    Locked = 2
}

/// <summary>WMS 单据状态</summary>
public enum WmsOrderStatus
{
    Draft = 0,
    Approved = 1,
    Executing = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>WMS 单据业务来源（各仅一套入/出库表，用本枚举区分）</summary>
public enum WmsOrderType
{
    Other = 0,
    Purchase = 1,
    Production = 2,
    EmptyPallet = 3
}
