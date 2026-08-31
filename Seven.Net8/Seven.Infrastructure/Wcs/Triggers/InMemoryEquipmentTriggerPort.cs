using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs.Triggers;

/// <summary>仿真/测试用设备触发端口：事件订阅 + 下发记录。</summary>
public sealed class InMemoryEquipmentTriggerPort : IEquipmentTriggerPort
{
    private readonly object _lock = new();
    private Func<DestinationRequestTrigger, Task>? _destinationRequested;
    private Func<DeviceSegmentFeedback, Task>? _segmentFeedback;

    public List<DispatchDestinationCommand> DispatchedDestinations { get; } = [];
    public List<RejectDestinationCommand> RejectedDestinations { get; } = [];
    public List<DispatchMoveCommand> DispatchedMoves { get; } = [];

    public event Func<DestinationRequestTrigger, Task>? DestinationRequested
    {
        add
        {
            lock (_lock)
            {
                _destinationRequested += value;
            }
        }
        remove
        {
            lock (_lock)
            {
                _destinationRequested -= value;
            }
        }
    }

    public event Func<DeviceSegmentFeedback, Task>? SegmentFeedback
    {
        add
        {
            lock (_lock)
            {
                _segmentFeedback += value;
            }
        }
        remove
        {
            lock (_lock)
            {
                _segmentFeedback -= value;
            }
        }
    }

    public async Task SimulateDestinationRequestAsync(
        DestinationRequestTrigger trigger,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Func<DestinationRequestTrigger, Task>? handler;
        lock (_lock)
        {
            handler = _destinationRequested;
        }

        if (handler != null)
        {
            foreach (var d in handler.GetInvocationList())
            {
                await ((Func<DestinationRequestTrigger, Task>)d)(trigger).ConfigureAwait(false);
            }
        }
    }

    public async Task SimulateSegmentFeedbackAsync(
        DeviceSegmentFeedback feedback,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Func<DeviceSegmentFeedback, Task>? handler;
        lock (_lock)
        {
            handler = _segmentFeedback;
        }

        if (handler != null)
        {
            foreach (var d in handler.GetInvocationList())
            {
                await ((Func<DeviceSegmentFeedback, Task>)d)(feedback).ConfigureAwait(false);
            }
        }
    }

    public Task DispatchDestinationAsync(DispatchDestinationCommand cmd, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            DispatchedDestinations.Add(cmd);
        }

        return Task.CompletedTask;
    }

    public Task RejectDestinationAsync(RejectDestinationCommand cmd, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            RejectedDestinations.Add(cmd);
        }

        return Task.CompletedTask;
    }

    public Task DispatchMoveAsync(DispatchMoveCommand cmd, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            DispatchedMoves.Add(cmd);
        }

        return Task.CompletedTask;
    }
}
