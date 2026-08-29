namespace Seven.Application.Wcs;

/// <summary>通讯无关：映射 LES_v2 的 SUDR/SUDS/SUM* 语义，由未来通讯包调用。</summary>
public interface IEquipmentTriggerPort
{
    event Func<DestinationRequestTrigger, Task>? DestinationRequested; // SUDR
    Task DispatchDestinationAsync(DispatchDestinationCommand cmd, CancellationToken ct = default); // SUDS
    Task DispatchMoveAsync(DispatchMoveCommand cmd, CancellationToken ct = default); // SUMT/SUMM/SUPM
    event Func<DeviceSegmentFeedback, Task>? SegmentFeedback; // SUMR/SUPR/SULL…

    Task SimulateDestinationRequestAsync(DestinationRequestTrigger trigger, CancellationToken ct = default);
    Task SimulateSegmentFeedbackAsync(DeviceSegmentFeedback feedback, CancellationToken ct = default);
}
