# Task 3 review package

### FILE: Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayDestinationService.cs
```csharp
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>璁㈤槄 SUDR/娈靛弽棣堬細鍥涘悜鍏ュ簱灞?宸?璐т綅鍒嗛厤涓庢嫆鏀讹紱F3 鍗曟鍙嶉鍗冲畬鎴?PutAway銆?/summary>
public sealed class FourWayDestinationService
{
    private readonly SevenDbContext _db;
    private readonly IEquipmentTriggerPort _port;
    private readonly FourWayInboundAllocator _allocator;
    private readonly IOrchestrationBus? _bus;
    private bool _subscribed;

    public FourWayDestinationService(
        SevenDbContext db,
        IEquipmentTriggerPort port,
        FourWayInboundAllocator allocator,
        IOrchestrationBus? bus = null)
    {
        _db = db;
        _port = port;
        _allocator = allocator;
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
        {
            var failedTask = await FindActivePutAwayAsync(trigger.ContainerCode, ct);
            await RejectAsync(trigger, "澶栧舰鎴栨牎楠屾湭閫氳繃: " + trigger.CheckResult, markFailed: true, failedTask, ct);
            return;
        }

        var point = await _db.FwRequestPoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsEnabled && x.Code == trigger.SourcePointCode, ct);
        if (point == null)
            return; // 闈炴湰鍖呯敵璇风偣锛堝彲鑳藉睘 Stacker锛夛紝涓嶆嫆鏀?

        var putaway = await FindActivePutAwayAsync(trigger.ContainerCode, ct);
        if (putaway == null)
        {
            await RejectAsync(trigger, "鏃犲尮閰嶅叆搴撲笂鏋朵换鍔?, markFailed: false, ct);
            return;
        }

        string? destPoint;
        switch (point.PointType)
        {
            case FwRequestPointType.LayerRequest:
            case FwRequestPointType.AisleRequest:
            {
                destPoint = await EnsureAisleAndEpAsync(putaway, point, trigger, ct);
                if (destPoint == null)
                    return;
                break;
            }

            case FwRequestPointType.LocationRequest:
            {
                destPoint = await EnsureLocationAsync(putaway, point, trigger, ct);
                if (destPoint == null)
                    return;
                break;
            }

            default:
                await RejectAsync(trigger, "涓嶆敮鎸佺殑鐢宠鐐圭被鍨?, markFailed: false, putaway, ct);
                return;
        }

        putaway.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(putaway.ContainerCode, destPoint, putaway.LegId), ct);
    }

    public async Task HandleSegmentFeedbackAsync(DeviceSegmentFeedback feedback, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        FwPutAwayTask? putaway = null;
        if (feedback.LegId is { } legId)
        {
            putaway = await _db.FwPutAwayTasks.FirstOrDefaultAsync(x =>
                x.LegId == legId
                && x.Status != FwPutAwayStatus.Completed
                && x.Status != FwPutAwayStatus.Cancelled
                && x.Status != FwPutAwayStatus.Failed, ct);
        }

        putaway ??= await FindActivePutAwayAsync(feedback.ContainerCode, ct);
        if (putaway == null)
            return;

        putaway.Status = FwPutAwayStatus.Completed;
        putaway.ModifyDate = DateTime.UtcNow;

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == putaway.LegId, ct);
        if (shuttle != null
            && shuttle.Status != FwShuttleTaskStatus.Completed
            && shuttle.Status != FwShuttleTaskStatus.Cancelled
            && shuttle.Status != FwShuttleTaskStatus.Failed)
        {
            shuttle.Status = FwShuttleTaskStatus.Completed;
            shuttle.ModifyDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        if (_bus != null)
            await _bus.OnLegEventAsync(new LegEvent(putaway.LegId, LegEventType.Completed), ct);
    }

    private async Task<string?> EnsureAisleAndEpAsync(
        FwPutAwayTask putaway,
        FwRequestPoint point,
        DestinationRequestTrigger trigger,
        CancellationToken ct)
    {
        var warehouseId = await ResolveWarehouseIdAsync(putaway, ct);
        if (warehouseId == null)
        {
            await RejectAsync(trigger, "鏃犳硶瑙ｆ瀽浠撳簱", markFailed: true, putaway, ct);
            return null;
        }

        var request = new AllocationRequest(
            warehouseId.Value,
            WcsPackIds.FourWay,
            Height: trigger.Height,
            Weight: trigger.Weight,
            PreferredLayerCode: putaway.AssignedLayer ?? point.LayerCode,
            PreferredAisleCode: putaway.AssignedAisle ?? point.AisleCode);

        if (string.IsNullOrWhiteSpace(putaway.AssignedLayer))
        {
            var layer = await _allocator.SelectLayerForRequestAsync(request, ct);
            if (layer == null)
            {
                await RejectAsync(trigger, "灞傚垎閰嶅け璐?, markFailed: true, putaway, ct);
                return null;
            }

            putaway.AssignedLayer = layer;
            putaway.Status = FwPutAwayStatus.LayerAssigned;
        }

        if (string.IsNullOrWhiteSpace(putaway.AssignedAisle))
        {
            var aisleReq = request with { PreferredLayerCode = putaway.AssignedLayer };
            var aisle = await _allocator.SelectAisleForRequestAsync(aisleReq, putaway.AssignedLayer!, ct);
            if (aisle == null)
            {
                await RejectAsync(trigger, "宸烽亾鍒嗛厤澶辫触", markFailed: true, putaway, ct);
                return null;
            }

            putaway.AssignedAisle = aisle;
            putaway.Status = FwPutAwayStatus.AisleAssigned;
        }
        else if (putaway.Status is FwPutAwayStatus.Accepted or FwPutAwayStatus.LayerAssigned)
        {
            putaway.Status = FwPutAwayStatus.AisleAssigned;
        }

        var ep = await _allocator.GetAisleDestinationPointAsync(
            putaway.AssignedLayer!, putaway.AssignedAisle!, ct);
        if (string.IsNullOrWhiteSpace(ep))
        {
            await RejectAsync(trigger, "宸烽亾缂哄皯鐩殑鍦扮偣", markFailed: true, putaway, ct);
            return null;
        }

        return ep;
    }

    private async Task<string?> EnsureLocationAsync(
        FwPutAwayTask putaway,
        FwRequestPoint point,
        DestinationRequestTrigger trigger,
        CancellationToken ct)
    {
        var warehouseId = await ResolveWarehouseIdAsync(putaway, ct);
        if (warehouseId == null)
        {
            await RejectAsync(trigger, "鏃犳硶瑙ｆ瀽浠撳簱", markFailed: true, putaway, ct);
            return null;
        }

        var layerCode = putaway.AssignedLayer ?? point.LayerCode;
        var aisleCode = putaway.AssignedAisle ?? point.AisleCode;
        if (string.IsNullOrWhiteSpace(layerCode) || string.IsNullOrWhiteSpace(aisleCode))
        {
            await RejectAsync(trigger, "璐т綅鐢宠缂哄皯灞傛垨宸烽亾", markFailed: true, putaway, ct);
            return null;
        }

        putaway.AssignedLayer = layerCode;
        putaway.AssignedAisle = aisleCode;

        if (!string.IsNullOrWhiteSpace(putaway.AssignedLocationCode))
        {
            var prev = await _db.WmsLocations
                .FirstOrDefaultAsync(x => x.Code == putaway.AssignedLocationCode, ct);
            if (prev != null)
            {
                prev.IsBooked = false;
                prev.ModifyDate = DateTime.UtcNow;
            }

            putaway.AssignedLocationCode = null;
        }

        var location = await _allocator.SelectAndBookLocationForRequestAsync(
            warehouseId.Value, layerCode, aisleCode, ct);
        if (location == null)
        {
            await RejectAsync(trigger, "璐т綅鍒嗛厤澶辫触", markFailed: true, putaway, ct);
            return null;
        }

        putaway.AssignedLocationCode = location;
        putaway.Status = FwPutAwayStatus.LocationAssigned;
        return location;
    }

    private async Task<int?> ResolveWarehouseIdAsync(FwPutAwayTask putaway, CancellationToken ct)
    {
        foreach (var code in new[] { putaway.AssignedLocationCode, putaway.ToCode, putaway.FromCode })
        {
            if (string.IsNullOrWhiteSpace(code))
                continue;
            var loc = await _db.WmsLocations.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code, ct);
            if (loc != null)
                return loc.WarehouseId;
        }

        if (!string.IsNullOrWhiteSpace(putaway.AssignedLayer))
        {
            var layer = await _db.WmsLayers.AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Code == putaway.AssignedLayer && x.PackId == WcsPackIds.FourWay, ct);
            if (layer != null)
                return layer.WarehouseId;
        }

        var wh = await _db.WmsWarehouses.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.EnabledPackIds != null
                && x.EnabledPackIds.Contains(WcsPackIds.FourWay), ct);
        return wh?.Id;
    }

    private Task<FwPutAwayTask?> FindActivePutAwayAsync(string containerCode, CancellationToken ct) =>
        _db.FwPutAwayTasks.FirstOrDefaultAsync(x => x.ContainerCode == containerCode
            && x.Status != FwPutAwayStatus.Completed
            && x.Status != FwPutAwayStatus.Cancelled
            && x.Status != FwPutAwayStatus.Failed, ct);

    private Task RejectAsync(
        DestinationRequestTrigger trigger,
        string reason,
        bool markFailed,
        CancellationToken ct) =>
        RejectAsync(trigger, reason, markFailed, putaway: null, ct);

    private async Task RejectAsync(
        DestinationRequestTrigger trigger,
        string reason,
        bool markFailed,
        FwPutAwayTask? putaway,
        CancellationToken ct)
    {
        Guid? legId = putaway?.LegId;
        if (putaway != null && markFailed)
        {
            putaway.Status = FwPutAwayStatus.Failed;
            putaway.ModifyDate = DateTime.UtcNow;

            var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == putaway.LegId, ct);
            if (shuttle != null
                && shuttle.Status != FwShuttleTaskStatus.Completed
                && shuttle.Status != FwShuttleTaskStatus.Cancelled
                && shuttle.Status != FwShuttleTaskStatus.Failed)
            {
                shuttle.Status = FwShuttleTaskStatus.Failed;
                shuttle.ModifyDate = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }

        await _port.RejectDestinationAsync(
            new RejectDestinationCommand(trigger.ContainerCode, reason, trigger.SourcePointCode, legId), ct);
    }
}
```

### FILE: Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWayWcsPack.cs
```csharp
using Microsoft.EntityFrameworkCore;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Domain.Wms;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>鍥涘悜杞?WCS 鍖咃細鍏ュ簱鍐?PutAway锛堟簮锛? 杞婚噺 ShuttleTask锛汣anHandle 浠?Fw./浜ゆ帴銆?/summary>
public sealed class FourWayWcsPack : IWcsPack
{
    private readonly SevenDbContext _db;
    private readonly IControlModeService _controlMode;

    public FourWayWcsPack(SevenDbContext db, IControlModeService controlMode)
    {
        _db = db;
        _controlMode = controlMode;
    }

    public string PackId => WcsPackIds.FourWay;

    public async Task<bool> CanHandleAsync(string fromLocationCode, string toLocationCode, CancellationToken ct = default)
    {
        var fromFw = PackCodeRules.HasValidPrefix(fromLocationCode, WcsPackIds.FourWay);
        var toFw = PackCodeRules.HasValidPrefix(toLocationCode, WcsPackIds.FourWay);
        if (fromFw && toFw)
            return true;

        // 绾爢鍨涘锛氱粷涓嶆帴鍗?
        var fromStk = PackCodeRules.HasValidPrefix(fromLocationCode, WcsPackIds.Stacker);
        var toStk = PackCodeRules.HasValidPrefix(toLocationCode, WcsPackIds.Stacker);
        if (fromStk && toStk)
            return false;

        var handover = await _db.WmsHandoverLinks.AsNoTracking().AnyAsync(x =>
            (x.FromPackId == WcsPackIds.FourWay || x.ToPackId == WcsPackIds.FourWay)
            && (x.LocationCode == fromLocationCode || x.LocationCode == toLocationCode), ct);
        return handover;
    }

    public async Task<AcceptLegResult> AcceptLegAsync(TransportLegDto leg, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(leg);
        if (!await _controlMode.CanAcceptLegsAsync(PackId, ct))
            return new AcceptLegResult(false, "鑱旈攣绂佹鎺ュ崟锛堟€ュ仠鎴栨墜鍔ㄦā寮忥級");

        var hasPutAway = await _db.FwPutAwayTasks.AnyAsync(x => x.LegId == leg.LegId, ct);
        var hasShuttle = await _db.FwShuttleTasks.AnyAsync(x => x.LegId == leg.LegId, ct);
        if (hasPutAway && hasShuttle)
            return new AcceptLegResult(true);

        var isOutbound = string.Equals(leg.RefType, "OutboundOrder", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(leg.RefType, "FourWayTransfer", StringComparison.OrdinalIgnoreCase);

        if (!hasPutAway && !isOutbound)
        {
            _db.FwPutAwayTasks.Add(new FwPutAwayTask
            {
                Id = Guid.NewGuid(),
                LegId = leg.LegId,
                ContainerCode = leg.ContainerCode,
                FromCode = leg.FromCode,
                ToCode = leg.ToCode,
                Status = FwPutAwayStatus.Accepted,
                CreateDate = DateTime.UtcNow
            });
        }

        if (!hasShuttle)
        {
            _db.FwShuttleTasks.Add(new FwShuttleTask
            {
                Id = Guid.NewGuid(),
                LegId = leg.LegId,
                ContainerCode = leg.ContainerCode,
                FromCode = leg.FromCode,
                ToCode = leg.ToCode,
                Status = FwShuttleTaskStatus.Accepted,
                CreateDate = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return new AcceptLegResult(true);
    }

    public async Task CancelLegAsync(Guid legId, CancellationToken ct = default)
    {
        var putaway = await _db.FwPutAwayTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (putaway != null)
        {
            putaway.Status = FwPutAwayStatus.Cancelled;
            putaway.ModifyDate = DateTime.UtcNow;
        }

        var shuttle = await _db.FwShuttleTasks.FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (shuttle != null)
        {
            shuttle.Status = FwShuttleTaskStatus.Cancelled;
            shuttle.ModifyDate = DateTime.UtcNow;
        }

        if (putaway != null || shuttle != null)
            await _db.SaveChangesAsync(ct);
    }

    public async Task<LegStatusDto?> QueryLegAsync(Guid legId, CancellationToken ct = default)
    {
        var putaway = await _db.FwPutAwayTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        if (putaway != null)
            return new LegStatusDto(legId, putaway.Status.ToString());

        var shuttle = await _db.FwShuttleTasks.AsNoTracking().FirstOrDefaultAsync(x => x.LegId == legId, ct);
        return shuttle == null ? null : new LegStatusDto(legId, shuttle.Status.ToString());
    }

    public async Task<PackHealthDto> HealthAsync(CancellationToken ct = default)
    {
        var canAccept = await _controlMode.CanAcceptLegsAsync(PackId, ct);
        return canAccept
            ? new PackHealthDto(PackId, true, true)
            : new PackHealthDto(PackId, false, false, "鑱旈攣绂佹鎺ュ崟锛堟€ュ仠鎴栨墜鍔ㄦā寮忥級");
    }
}
```

### FILE: Seven.Net8/Seven.Infrastructure/Wcs/Packs/FourWay/FourWaySchedulerHostedService.cs
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Seven.Application.Wcs;

namespace Seven.Infrastructure.Wcs.Packs.FourWay;

/// <summary>钖勫涓伙細鎶?Singleton 绔彛浜嬩欢杞埌 Scoped DestinationService銆?/summary>
public sealed class FourWaySchedulerHostedService : IHostedService
{
    private readonly IEquipmentTriggerPort _port;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FourWaySchedulerHostedService> _logger;

    public FourWaySchedulerHostedService(
        IEquipmentTriggerPort port,
        IServiceScopeFactory scopeFactory,
        ILogger<FourWaySchedulerHostedService> logger)
    {
        _port = port;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _port.DestinationRequested += OnDestinationRequestedAsync;
        _port.SegmentFeedback += OnSegmentFeedbackAsync;
        _logger.LogInformation("FourWay destination subscriptions started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _port.DestinationRequested -= OnDestinationRequestedAsync;
        _port.SegmentFeedback -= OnSegmentFeedbackAsync;
        return Task.CompletedTask;
    }

    private async Task OnDestinationRequestedAsync(DestinationRequestTrigger trigger)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dest = scope.ServiceProvider.GetRequiredService<FourWayDestinationService>();
            await dest.HandleDestinationRequestedAsync(trigger);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FourWay DestinationRequested handling failed for {Container}", trigger.ContainerCode);
        }
    }

    private async Task OnSegmentFeedbackAsync(DeviceSegmentFeedback feedback)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dest = scope.ServiceProvider.GetRequiredService<FourWayDestinationService>();
            await dest.HandleSegmentFeedbackAsync(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FourWay SegmentFeedback handling failed for {Container}", feedback.ContainerCode);
        }
    }
}
```

### FILE: Seven.Net8/Seven.Infrastructure/Wcs/WcsServiceCollectionExtensions.cs
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Seven.Application.Interfaces;
using Seven.Application.Platform;
using Seven.Application.Wcs;
using Seven.Application.Wms;
using Seven.Infrastructure.Configuration;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Bus;
using Seven.Infrastructure.Wcs.External;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Packs.Stacker;
using Seven.Infrastructure.Wcs.Triggers;
using Seven.Infrastructure.Wms;

namespace Seven.Infrastructure.Wcs;

/// <summary>WCS/鎬荤嚎/鍖?DI銆傚缁堟敞鍐岋紱Features 浠呮帶鍒跺墠绔彍鍗曟樉绀恒€?/summary>
public static class WcsServiceCollectionExtensions
{
    public static IServiceCollection AddSevenWcs(this IServiceCollection services, IConfiguration configuration)
    {
        var externalEntries = configuration.GetSection(ExternalWcsEntryOptions.SectionName)
            .Get<List<ExternalWcsEntryOptions>>() ?? [];

        services.AddSingleton<IEquipmentTriggerPort, InMemoryEquipmentTriggerPort>();
        services.AddScoped<IBusTransportOrderQuery, BusTransportOrderQuery>();

        services.AddHttpClient(ExternalWcsHttpClient.Name);
        services.AddSingleton<MqExternalTransport>();
        services.AddSingleton<ExternalTransportFactory>();
        services.AddSingleton<IVendorCodec, FakeVendorCodec>();
        services.AddSingleton<VendorCodecRegistry>();
        services.AddScoped<ExternalWcsPackFactory>();

        foreach (var entry in externalEntries.Where(x => x.Enabled))
        {
            var captured = entry;
            services.AddScoped<IWcsPack>(sp =>
                sp.GetRequiredService<ExternalWcsPackFactory>().Create(captured));
        }

        services.AddScoped<IWcsPackResolver, WcsPackResolver>();
        services.AddScoped<OrchestrationBus>();
        services.AddScoped<IOrchestrationBus>(sp => sp.GetRequiredService<OrchestrationBus>());
        services.AddScoped<IWmsTransportCompletionHandler, WmsTransportCompletionHandler>();
        services.AddHostedService<OrchestrationBusHostedService>();

        services.AddScoped<StackerAisleAllocator>();
        services.AddScoped<StackerLocationAllocator>();
        services.AddScoped<StackerPathDispatcher>();
        services.AddScoped(sp => new StackerDepthGuard(
            sp.GetRequiredService<SevenDbContext>(),
            sp));
        services.AddScoped<StackerLocationSchema>();
        services.AddScoped<IWcsLocationSchema>(sp => sp.GetRequiredService<StackerLocationSchema>());
        services.AddScoped<StackerInboundAllocator>();
        services.AddScoped<IWcsLocationAllocator>(sp => sp.GetRequiredService<StackerInboundAllocator>());
        services.AddScoped<StackerWcsPack>(sp => new StackerWcsPack(
            sp.GetRequiredService<SevenDbContext>(),
            sp.GetRequiredService<IControlModeService>(),
            sp.GetRequiredService<IEquipmentTriggerPort>(),
            sp.GetRequiredService<StackerPathDispatcher>(),
            sp.GetRequiredService<StackerDepthGuard>()));
        services.AddScoped<IWcsPack>(sp => sp.GetRequiredService<StackerWcsPack>());
        services.AddScoped(sp => new StackerDestinationService(
            sp.GetRequiredService<SevenDbContext>(),
            sp.GetRequiredService<IEquipmentTriggerPort>(),
            sp.GetRequiredService<StackerAisleAllocator>(),
            sp.GetRequiredService<StackerLocationAllocator>(),
            sp.GetRequiredService<StackerPathDispatcher>(),
            sp.GetService<IOrchestrationBus>(),
            sp.GetRequiredService<StackerWcsPack>()));
        services.AddHostedService<StackerSchedulerHostedService>();

        services.AddSingleton(sp => new FourWayTrafficGuard(sp.GetService<IHotStore>()));
        services.AddScoped<FourWayLocationSchema>();
        services.AddScoped<IWcsLocationSchema>(sp => sp.GetRequiredService<FourWayLocationSchema>());
        services.AddScoped<FourWayInboundAllocator>();
        services.AddScoped<IWcsLocationAllocator>(sp => sp.GetRequiredService<FourWayInboundAllocator>());
        services.AddScoped<FourWayWcsPack>();
        services.AddScoped<IWcsPack>(sp => sp.GetRequiredService<FourWayWcsPack>());
        services.AddScoped(sp => new FourWayDestinationService(
            sp.GetRequiredService<SevenDbContext>(),
            sp.GetRequiredService<IEquipmentTriggerPort>(),
            sp.GetRequiredService<FourWayInboundAllocator>(),
            sp.GetService<IOrchestrationBus>()));
        services.AddHostedService<FourWaySchedulerHostedService>();

        services.AddScoped<IWcsLocationAllocatorResolver, WcsLocationAllocatorResolver>();

        return services;
    }
}
```

### FILE: D:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Net8\Seven.Tests\Wcs\FourWayAllocatorTests.cs
```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Packs.FourWay;

namespace Seven.Tests.Wcs;

public class FourWayAllocatorTests
{
    [Fact]
    public async Task SelectLayer_ShouldPrefer_HigherAllocationWeight()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedLayerPolicy(db, wh.Code, "Fw.L02", weight: 10);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 0);
        SeedAislePolicy(db, "Fw.L02", "Fw.A-L02", minEmpty: 0);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-L02", "Fw.N-L02-01");
        await db.SaveChangesAsync();

        var result = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10));

        result.Ok.Should().BeTrue();
        result.LayerCode.Should().Be("Fw.L02");
        result.LocationCode.Should().Be("Fw.N-L02-01");
    }

    [Fact]
    public async Task SelectAisle_ShouldSkip_WhenMinEmptySlotsNotMet()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 2);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        await db.SaveChangesAsync();

        var result = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10));

        result.Ok.Should().BeFalse();
        result.Message.Should().Contain("宸?);
    }

    [Fact]
    public async Task SelectLocation_ShouldBook_IsBooked()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 0);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        await db.SaveChangesAsync();

        var result = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10));

        result.Ok.Should().BeTrue();
        result.LocationCode.Should().Be("Fw.N-L01-01");
        (await db.WmsLocations.SingleAsync(x => x.Code == "Fw.N-L01-01")).IsBooked.Should().BeTrue();
    }

    [Fact]
    public async Task SelectAisle_ShouldUseSelectedLayerOnly_WhenAisleCodeSharedAcrossLayers()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 1);
        SeedLayerPolicy(db, wh.Code, "Fw.L02", weight: 1);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-SHARED", minEmpty: 2);
        SeedAislePolicy(db, "Fw.L02", "Fw.A-SHARED", minEmpty: 0);

        // L01 浠?1 绌轰綅锛屽崟鐙笉婊¤冻 MinEmpty=2锛汱02 澶氱┖浣嶄笉寰椾覆鍏?L01 璁℃暟
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-SHARED", "Fw.N-L01-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-SHARED", "Fw.N-L02-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-SHARED", "Fw.N-L02-02");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-SHARED", "Fw.N-L02-03");

        // L02 鍚?Code 宸烽亾涓嶅彲鐢細鑻ユ湭鎸?LayerId 杩囨护锛孴oDictionary 鍙兘璇激 L01
        var l02 = await db.WmsLayers.SingleAsync(x => x.Code == "Fw.L02");
        var aisleL02 = await db.WmsAisles.SingleAsync(x => x.Code == "Fw.A-SHARED" && x.LayerId == l02.Id);
        aisleL02.IsAvailable = false;
        await db.SaveChangesAsync();

        var fail = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10, PreferredLayerCode: "Fw.L01"));
        fail.Ok.Should().BeFalse();
        fail.Message.Should().Contain("宸?);

        // L01 琛ヨ冻绌轰綅鍚庡簲鎴愬姛锛汱02 涓嶅彲鐢ㄤ笉寰楅樆濉炴湰灞?
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-SHARED", "Fw.N-L01-02");
        await db.SaveChangesAsync();

        var ok = await new FourWayInboundAllocator(db).AllocateInboundAsync(
            new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10, PreferredLayerCode: "Fw.L01"));
        ok.Ok.Should().BeTrue();
        ok.LayerCode.Should().Be("Fw.L01");
        ok.AisleCode.Should().Be("Fw.A-SHARED");
        ok.LocationCode.Should().BeOneOf("Fw.N-L01-01", "Fw.N-L01-02");
    }

    [Fact]
    public async Task SelectLayer_ShouldRotate_UsingFwAssignmentRecord()
    {
        var db = CreateDb();
        var wh = SeedWarehouse(db);
        SeedLayerPolicy(db, wh.Code, "Fw.L01", weight: 5);
        SeedLayerPolicy(db, wh.Code, "Fw.L02", weight: 5);
        SeedAislePolicy(db, "Fw.L01", "Fw.A-L01", minEmpty: 0);
        SeedAislePolicy(db, "Fw.L02", "Fw.A-L02", minEmpty: 0);
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-01");
        SeedLocation(db, wh.Id, "Fw.L01", "Fw.A-L01", "Fw.N-L01-02");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-L02", "Fw.N-L02-01");
        SeedLocation(db, wh.Id, "Fw.L02", "Fw.A-L02", "Fw.N-L02-02");
        await db.SaveChangesAsync();

        var allocator = new FourWayInboundAllocator(db);
        var req = new AllocationRequest(wh.Id, WcsPackIds.FourWay, Height: 1, Weight: 10);

        var first = await allocator.AllocateInboundAsync(req);
        var second = await allocator.AllocateInboundAsync(req);
        var third = await allocator.AllocateInboundAsync(req);

        first.Ok.Should().BeTrue();
        second.Ok.Should().BeTrue();
        third.Ok.Should().BeTrue();
        first.LayerCode.Should().BeOneOf("Fw.L01", "Fw.L02");
        second.LayerCode.Should().BeOneOf("Fw.L01", "Fw.L02");
        first.LayerCode.Should().NotBe(second.LayerCode);
        third.LayerCode.Should().Be(first.LayerCode);

        var records = await db.FwAssignmentRecords
            .Where(x => x.ScopeType == FwAssignmentScopeType.Layer)
            .ToListAsync();
        records.Should().HaveCount(2);
        records.Should().OnlyContain(x => x.AssignCount >= 1);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"FwAlloc_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static WmsWarehouse SeedWarehouse(SevenDbContext db)
    {
        var wh = new WmsWarehouse
        {
            Code = "WH-FW",
            Name = "鍥涘悜浠?,
            EnabledPackIds = WcsPackIds.FourWay,
            CreateDate = DateTime.UtcNow
        };
        db.WmsWarehouses.Add(wh);
        db.SaveChanges();
        return wh;
    }

    private static void SeedLayerPolicy(SevenDbContext db, string warehouseCode, string layerCode, int weight)
    {
        db.FwLayerPolicies.Add(new FwLayerPolicy
        {
            WarehouseCode = warehouseCode,
            ZoneCode = "Fw.Z",
            LayerCode = layerCode,
            MaxHeight = 9999,
            MaxWeight = 99999,
            IsAvailable = true,
            AllocationWeight = weight,
            CreateDate = DateTime.UtcNow
        });
    }

    private static void SeedAislePolicy(
        SevenDbContext db,
        string layerCode,
        string aisleCode,
        int minEmpty)
    {
        db.FwAislePolicies.Add(new FwAislePolicy
        {
            LayerCode = layerCode,
            AisleCode = aisleCode,
            MinEmptySlots = minEmpty,
            MaxShuttleCount = 0,
            DestinationPointCode = $"EP-{aisleCode}",
            AllocationWeight = 1,
            IsAvailable = true,
            MaxHeight = 9999,
            MaxWeight = 99999,
            CreateDate = DateTime.UtcNow
        });
    }

    private static void SeedLocation(
        SevenDbContext db,
        int warehouseId,
        string layerCode,
        string aisleCode,
        string locationCode)
    {
        var layer = db.WmsLayers.Local.FirstOrDefault(x => x.Code == layerCode)
                    ?? db.WmsLayers.FirstOrDefault(x => x.Code == layerCode);
        if (layer == null)
        {
            layer = new WmsLayer
            {
                WarehouseId = warehouseId,
                ZoneId = 0,
                PackId = WcsPackIds.FourWay,
                Code = layerCode,
                Name = layerCode,
                IsAvailable = true,
                CreateDate = DateTime.UtcNow
            };
            db.WmsLayers.Add(layer);
            db.SaveChanges();
        }

        var aisle = db.WmsAisles.Local.FirstOrDefault(x => x.Code == aisleCode && x.LayerId == layer.Id)
                    ?? db.WmsAisles.FirstOrDefault(x => x.Code == aisleCode && x.LayerId == layer.Id);
        if (aisle == null)
        {
            aisle = new WmsAisle
            {
                WarehouseId = warehouseId,
                ZoneId = 0,
                LayerId = layer.Id,
                PackId = WcsPackIds.FourWay,
                Code = aisleCode,
                Name = aisleCode,
                IsAvailable = true,
                CreateDate = DateTime.UtcNow
            };
            db.WmsAisles.Add(aisle);
            db.SaveChanges();
        }

        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = warehouseId,
            LayerId = layer.Id,
            AisleId = aisle.Id,
            PackId = WcsPackIds.FourWay,
            Code = locationCode,
            Aisle = aisleCode,
            CreateDate = DateTime.UtcNow
        });
    }
}
```

### FILE: D:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Net8\Seven.Tests\Wcs\FourWayPackTests.cs
```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Enums;
using Seven.Domain.Wcs;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Platform;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Triggers;

namespace Seven.Tests.Wcs;

public class FourWayPackTests
{
    [Fact]
    public void Router_ShouldFindPath_OnThreeNodeGraph()
    {
        var edges = new[]
        {
            new FourWayEdge("A", "C", Weight: 1),
            new FourWayEdge("C", "B", Weight: 1)
        };

        var path = FourWayRouter.FindPath("A", "B", edges);

        path.Should().Equal("A", "C", "B");
    }

    [Fact]
    public async Task Traffic_ShouldReject_HeadOnOppositeDirection()
    {
        var guard = new FourWayTrafficGuard();

        var first = await guard.TryGrantAsync("e-ab", fromNode: "A", toNode: "B", ownerId: "v1");
        var opposite = await guard.TryGrantAsync("e-ab", fromNode: "B", toNode: "A", ownerId: "v2");

        first.Should().BeTrue();
        opposite.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptLeg_ShouldCreatePutAwayAndShuttle_WhenCanHandle()
    {
        var db = CreateDb();
        var pack = new FourWayWcsPack(db, new ControlModeService(db));

        pack.PackId.Should().Be(WcsPackIds.FourWay);
        (await pack.CanHandleAsync("Fw.A", "Fw.B")).Should().BeTrue();
        (await pack.CanHandleAsync("Stk.A", "Stk.B")).Should().BeFalse();

        var leg = new TransportLegDto(
            Guid.NewGuid(), Guid.NewGuid(), WcsPackIds.FourWay, 1, "Fw.A", "Fw.B", "TP-FW-1", null, null);

        var result = await pack.AcceptLegAsync(leg);

        result.Accepted.Should().BeTrue();
        var putaway = await db.FwPutAwayTasks.SingleAsync(x => x.LegId == leg.LegId);
        putaway.ContainerCode.Should().Be("TP-FW-1");
        putaway.Status.Should().Be(FwPutAwayStatus.Accepted);
        var shuttle = await db.FwShuttleTasks.SingleAsync(x => x.LegId == leg.LegId);
        shuttle.ContainerCode.Should().Be("TP-FW-1");
        shuttle.Status.Should().Be(FwShuttleTaskStatus.Accepted);
    }

    [Fact]
    public async Task CanHandle_ShouldAllow_HandoverEnd()
    {
        var db = CreateDb();
        db.WmsHandoverLinks.Add(new WmsHandoverLink
        {
            FromPackId = WcsPackIds.Stacker,
            ToPackId = WcsPackIds.FourWay,
            LocationCode = "HO-FW-01",
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var pack = new FourWayWcsPack(db, new ControlModeService(db));
        (await pack.CanHandleAsync("HO-FW-01", "Fw.BIN-01")).Should().BeTrue();
        (await pack.CanHandleAsync("RECV", "Fw.BIN-01")).Should().BeFalse();
    }

    [Fact]
    public async Task SimulateDestinationRequest_CheckNg_ShouldReject()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        SeedFourWayMaster(db);
        await db.SaveChangesAsync();

        var pack = new FourWayWcsPack(db, new ControlModeService(db));
        var dest = new FourWayDestinationService(db, port, new FourWayInboundAllocator(db));
        dest.Subscribe();

        await pack.AcceptLegAsync(NewLeg("TP-NG", "Fw.RECV", "Fw.LOC-A1-01"));
        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-NG", "RP_FW_AISLE", 1, 10, "NG"));

        port.DispatchedDestinations.Should().BeEmpty();
        port.RejectedDestinations.Should().ContainSingle(x => x.Reason.Contains("鏍￠獙鏈€氳繃"));
        (await db.FwPutAwayTasks.SingleAsync(x => x.ContainerCode == "TP-NG"))
            .Status.Should().Be(FwPutAwayStatus.Failed);
    }

    [Fact]
    public async Task AisleRequest_ShouldDispatchEpPoint()
    {
        var db = CreateDb();
        var port = new InMemoryEquipmentTriggerPort();
        SeedFourWayMaster(db);
        await db.SaveChangesAsync();

        var pack = new FourWayWcsPack(db, new ControlModeService(db));
        var dest = new FourWayDestinationService(db, port, new FourWayInboundAllocator(db));
        dest.Subscribe();

        var leg = NewLeg("TP-AISLE", "Fw.RECV", "Fw.LOC-A1-01");
        var accepted = await pack.AcceptLegAsync(leg);
        accepted.Accepted.Should().BeTrue();

        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP-AISLE", "RP_FW_AISLE", 1, 10, "OK"));

        port.DispatchedDestinations.Should().ContainSingle();
        var cmd = port.DispatchedDestinations.Single();
        cmd.ContainerCode.Should().Be("TP-AISLE");
        cmd.DestinationPointCode.Should().Be("EP-Fw.A1");
        cmd.LegId.Should().Be(leg.LegId);

        var putaway = await db.FwPutAwayTasks.SingleAsync(x => x.ContainerCode == "TP-AISLE");
        putaway.Status.Should().Be(FwPutAwayStatus.AisleAssigned);
        putaway.AssignedLayer.Should().Be("Fw.L01");
        putaway.AssignedAisle.Should().Be("Fw.A1");
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Fw_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static TransportLegDto NewLeg(string container, string from, string to)
        => new(Guid.NewGuid(), Guid.NewGuid(), WcsPackIds.FourWay, 1, from, to, container, null, null);

    private static void SeedFourWayMaster(SevenDbContext db)
    {
        var wh = new WmsWarehouse
        {
            Code = "WH-FW",
            Name = "鍥涘悜浠?,
            EnabledPackIds = WcsPackIds.FourWay,
            CreateDate = DateTime.UtcNow
        };
        db.WmsWarehouses.Add(wh);
        db.SaveChanges();

        var layer = new WmsLayer
        {
            WarehouseId = wh.Id,
            ZoneId = 0,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.L01",
            Name = "L01",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        };
        db.WmsLayers.Add(layer);
        db.SaveChanges();

        db.WmsAisles.Add(new WmsAisle
        {
            WarehouseId = wh.Id,
            ZoneId = 0,
            LayerId = layer.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.A1",
            Name = "A1",
            IsAvailable = true,
            CreateDate = DateTime.UtcNow
        });

        db.FwLayerPolicies.Add(new FwLayerPolicy
        {
            WarehouseCode = wh.Code,
            ZoneCode = "Fw.Z",
            LayerCode = "Fw.L01",
            MaxHeight = 9999,
            MaxWeight = 99999,
            IsAvailable = true,
            AllocationWeight = 10,
            CreateDate = DateTime.UtcNow
        });

        db.FwAislePolicies.Add(new FwAislePolicy
        {
            LayerCode = "Fw.L01",
            AisleCode = "Fw.A1",
            MinEmptySlots = 0,
            MaxShuttleCount = 0,
            DestinationPointCode = "EP-Fw.A1",
            AllocationWeight = 1,
            IsAvailable = true,
            MaxHeight = 9999,
            MaxWeight = 99999,
            CreateDate = DateTime.UtcNow
        });

        db.FwRequestPoints.Add(new FwRequestPoint
        {
            Code = "RP_FW_AISLE",
            PointType = FwRequestPointType.AisleRequest,
            IsEnabled = true,
            CreateDate = DateTime.UtcNow
        });

        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = wh.Id,
            LayerId = layer.Id,
            PackId = WcsPackIds.FourWay,
            Code = "Fw.LOC-A1-01",
            Aisle = "Fw.A1",
            CreateDate = DateTime.UtcNow
        });
    }
}
```
