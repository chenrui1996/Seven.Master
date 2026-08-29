namespace Seven.Domain.Enums;

/// <summary>堆垛机申请点类型（LES_v2 RequestPoint 净化）。</summary>
public enum StkRequestPointType
{
    AisleRequest = 0,
    LocationRequest = 1,
    BlockingPoint = 2,
    AP = 3,
    EP = 4
}

/// <summary>堆垛机上架任务状态。</summary>
public enum StkPutAwayStatus
{
    Accepted = 0,
    AisleAssigned = 1,
    LocationAssigned = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}

/// <summary>堆垛机设备段任务状态。</summary>
public enum StkDeviceTaskStatus
{
    Created = 0,
    Dispatched = 1,
    Completed = 2,
    Failed = 3
}
