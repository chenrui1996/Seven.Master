namespace Seven.Domain.Enums;

/// <summary>编排总线运输单状态。</summary>
public enum BusOrderStatus
{
    Created = 0,
    Planning = 1,
    Executing = 2,
    Completed = 3,
    Failed = 4,
    Cancelling = 5
}

/// <summary>编排总线 Leg 状态。</summary>
public enum BusLegStatus
{
    Pending = 0,
    Accepted = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}
