namespace Seven.Domain.Enums;

/// <summary>四向车穿梭任务状态。</summary>
public enum FwShuttleTaskStatus
{
    Accepted = 0,
    Routing = 1,
    Running = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}

/// <summary>四向入库上架任务状态。</summary>
public enum FwPutAwayStatus
{
    Accepted = 0,
    LayerAssigned = 1,
    AisleAssigned = 2,
    LocationAssigned = 3,
    Completed = 4,
    Cancelled = 5,
    Failed = 6
}

/// <summary>四向出库取货任务状态。</summary>
public enum FwRetrievalStatus
{
    Accepted = 0,
    Dispatched = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4,
    Suspended = 5
}

/// <summary>四向停车账本状态。</summary>
public enum FwParkingStatus
{
    Free = 0,
    Reserved = 1,
    Occupied = 2
}

/// <summary>四向目的地申请点类型。</summary>
public enum FwRequestPointType
{
    LayerRequest = 0,
    AisleRequest = 1,
    LocationRequest = 2,
    HoistInboundEp = 10,
    HoistInboundAp = 11,
    HoistOutboundEp = 12,
    HoistOutboundAp = 13,
    ShuttleEp = 20,
    ShuttleAp = 21
}

/// <summary>四向分配轮转记录范围（层 / 巷）。</summary>
public enum FwAssignmentScopeType
{
    Layer = 0,
    Aisle = 1
}

/// <summary>提升机业务任务状态。</summary>
public enum FwHoistTaskStatus
{
    Accepted = 0,
    Running = 1,
    Completed = 2,
    Cancelled = 3,
    Failed = 4
}

/// <summary>跨层包内阶段（单 Bus Leg）。</summary>
public enum FwHoistStage
{
    /// <summary>源层 Shuttle：货位 → 本层 Hoist AP</summary>
    ToSrcAp = 0,
    /// <summary>HoistExec：源层 → 目标层</summary>
    HoistLift = 1,
    /// <summary>目标层 Shuttle：Hoist EP → 目标货位</summary>
    FromDesEp = 2,
    Done = 3
}

/// <summary>提升机执行任务状态（排队 / 联锁挂起 / 下发）。</summary>
public enum FwHoistExecStatus
{
    Queued = 0,
    Suspended = 1,
    Dispatched = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}
