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
