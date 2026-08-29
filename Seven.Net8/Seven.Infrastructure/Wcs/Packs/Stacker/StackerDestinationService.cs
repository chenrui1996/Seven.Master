using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

/// <summary>订阅 SUDR/段反馈：巷道或货位分配后 SUDS 下发，完成后上报 Leg Completed。</summary>
public sealed class StackerDestinationService
{
    private readonly SevenDbContext _db;
    private readonly IEquipmentTriggerPort _port;
    private readonly StackerAisleAllocator _aisleAllocator;
    private readonly StackerLocationAllocator _locationAllocator;
    private readonly IOrchestrationBus? _bus;
    private bool _subscribed;

    public StackerDestinationService(
        SevenDbContext db,
        IEquipmentTriggerPort port,
        StackerAisleAllocator aisleAllocator,
        StackerLocationAllocator locationAllocator,
        IOrchestrationBus? bus = null)
    {
        _db = db;
        _port = port;
        _aisleAllocator = aisleAllocator;
        _locationAllocator = locationAllocator;
        _bus = bus;
    }

    public void Subscribe()
    {
        if (_subscribed)
            return;
        _port.DestinationRequested += OnDestinationRequestedAsync;
        _port.SegmentFeedback += OnSegmentFeedbackAsync;
        _subscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_subscribed)
            return;
        _port.DestinationRequested -= OnDestinationRequestedAsync;
        _port.SegmentFeedback -= OnSegmentFeedbackAsync;
        _subscribed = false;
    }

    public Task OnDestinationRequestedAsync(DestinationRequestTrigger trigger)
        => HandleDestinationRequestedAsync(trigger);

    public Task OnSegmentFeedbackAsync(DeviceSegmentFeedback feedback)
        => HandleSegmentFeedbackAsync(feedback);

    public async Task HandleDestinationRequestedAsync(DestinationRequestTrigger trigger, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        if (!string.Equals(trigger.CheckResult, "OK", StringComparison.OrdinalIgnoreCase))
            return;

        var point = await _db.StkRequestPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
        if (point == null)
            return;

        var putaway = await _db.StkPutAwayTasks
            .FirstOrDefaultAsync(x => x.ContainerCode == trigger.ContainerCode
                && x.Status != StkPutAwayStatus.Completed
                && x.Status != StkPutAwayStatus.Cancelled
                && x.Status != StkPutAwayStatus.Failed, ct);
        if (putaway == null)
            return;

        string? destPoint = null;
        switch (point.PointType)
        {
            case StkRequestPointType.AisleRequest:
                var aisle = await _aisleAllocator.SelectAisleAsync(trigger.Height, trigger.Weight, ct);
                if (aisle == null)
                    return;
                putaway.AssignedAisle = aisle.AisleCode;
                putaway.Status = StkPutAwayStatus.AisleAssigned;
                destPoint = aisle.DestinationPointCode;
                break;

            case StkRequestPointType.LocationRequest:
                var aisleCode = putaway.AssignedAisle ?? point.AisleCode;
                if (string.IsNullOrWhiteSpace(aisleCode))
                    return;
                var location = await _locationAllocator.SelectLocationAsync(aisleCode, ct);
                if (location == null)
                    return;
                putaway.AssignedLocationCode = location;
                putaway.Status = StkPutAwayStatus.LocationAssigned;
                destPoint = location;
                break;

            default:
                return;
        }

        putaway.ModifyDate = DateTime.UtcNow;
        var device = new StkDeviceTask
        {
            Id = Guid.NewGuid(),
            PutAwayTaskId = putaway.Id,
            LegId = putaway.LegId,
            ContainerCode = putaway.ContainerCode,
            DestinationPointCode = destPoint!,
            Status = StkDeviceTaskStatus.Dispatched,
            CreateDate = DateTime.UtcNow
        };
        _db.StkDeviceTasks.Add(device);
        await _db.SaveChangesAsync(ct);

        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(putaway.ContainerCode, destPoint!, putaway.LegId), ct);
    }

    public async Task HandleSegmentFeedbackAsync(DeviceSegmentFeedback feedback, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        var deviceQuery = _db.StkDeviceTasks.Where(x =>
            x.Status != StkDeviceTaskStatus.Completed && x.Status != StkDeviceTaskStatus.Failed);
        if (feedback.LegId is { } legId)
            deviceQuery = deviceQuery.Where(x => x.LegId == legId);
        else
            deviceQuery = deviceQuery.Where(x => x.ContainerCode == feedback.ContainerCode);

        var device = await deviceQuery.OrderByDescending(x => x.CreateDate).FirstOrDefaultAsync(ct);
        if (device == null)
            return;

        device.Status = StkDeviceTaskStatus.Completed;
        device.ModifyDate = DateTime.UtcNow;

        var putaway = await _db.StkPutAwayTasks.FirstOrDefaultAsync(x => x.Id == device.PutAwayTaskId, ct);
        if (putaway != null)
        {
            putaway.Status = StkPutAwayStatus.Completed;
            putaway.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        if (_bus != null)
        {
            await _bus.OnLegEventAsync(new LegEvent(device.LegId, LegEventType.Completed), ct);
        }
    }
}
