using Microsoft.EntityFrameworkCore;
using Seven.Application.Wcs;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;

namespace Seven.Infrastructure.Wcs.Packs.Stacker;

public sealed record StackerPathDispatchRequest(
    string FromPointCode,
    string ToPointCode,
    string ContainerCode,
    Guid LegId,
    Guid? PutAwayTaskId,
    Guid? RetrievalTaskId,
    string? MapCode = null);

/// <summary>JudgeMap + 寻路 + CreateTasks（同 ExeStackCode 合并）+ 首段下发与占流。</summary>
public sealed class StackerPathDispatcher
{
    private readonly SevenDbContext _db;
    private readonly IEquipmentTriggerPort _port;

    public StackerPathDispatcher(SevenDbContext db, IEquipmentTriggerPort port)
    {
        _db = db;
        _port = port;
    }

    public async Task<IReadOnlyList<StkDeviceTask>> DispatchAsync(
        StackerPathDispatchRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var from = await ResolvePointAsync(request.FromPointCode, ct);
        var to = await ResolvePointAsync(request.ToPointCode, ct);

        var map = request.MapCode ?? string.Empty;
        var routes = await _db.StkRoutes.AsNoTracking()
            .Where(x => x.IsEnabled && x.MapCode == map)
            .ToListAsync(ct);

        if (routes.Count == 0
            || string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return await DispatchSingleAsync(request, from, to, ct);
        }

        var routeIds = routes.Select(x => x.Id).ToList();
        var flowCounts = await _db.StkRouteFlows.AsNoTracking()
            .Where(x => routeIds.Contains(x.RouteId))
            .GroupBy(x => x.RouteId)
            .Select(g => new { RouteId = g.Key, Cnt = g.Count() })
            .ToListAsync(ct);
        var occ = flowCounts.ToDictionary(x => x.RouteId, x => x.Cnt);

        var edges = routes
            .Select(r => new StackerRouteEdge(
                r.Id,
                r.FromCode,
                r.ToCode,
                r.Weight,
                r.ExeStackCode,
                r.Capacity,
                occ.GetValueOrDefault(r.Id)))
            .ToList();

        var pathRouteIds = StackerRouter.FindPathRouteIds(from, to, edges);
        if (pathRouteIds.Count == 0)
            return await DispatchSingleAsync(request, from, to, ct);

        var byId = routes.ToDictionary(x => x.Id);
        var ordered = pathRouteIds.Select(id => byId[id]).ToList();
        var segments = MergeSegments(ordered);

        var tasks = new List<StkDeviceTask>();
        var seq = 1;
        foreach (var seg in segments)
        {
            var task = new StkDeviceTask
            {
                Id = Guid.NewGuid(),
                PutAwayTaskId = request.PutAwayTaskId,
                RetrievalTaskId = request.RetrievalTaskId,
                LegId = request.LegId,
                ContainerCode = request.ContainerCode,
                FromPointCode = seg.From,
                DestinationPointCode = seg.To,
                ExeStackCode = seg.ExeStackCode,
                Seq = seq++,
                Status = StkDeviceTaskStatus.Created,
                CreateDate = DateTime.UtcNow
            };
            tasks.Add(task);
            _db.StkDeviceTasks.Add(task);

            foreach (var routeId in seg.RouteIds)
            {
                _db.StkRouteFlows.Add(new StkRouteFlow
                {
                    RouteId = routeId,
                    DeviceTaskId = task.Id,
                    LegId = request.LegId,
                    ContainerCode = request.ContainerCode,
                    CreateDate = DateTime.UtcNow
                });
            }
        }

        var first = tasks[0];
        first.Status = StkDeviceTaskStatus.Dispatched;
        await _db.SaveChangesAsync(ct);
        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(first.ContainerCode, first.DestinationPointCode, first.LegId), ct);
        return tasks;
    }

    public async Task<bool> AdvanceAfterSegmentAsync(StkDeviceTask completed, CancellationToken ct = default)
    {
        await ReleaseFlowsAsync(completed.Id, ct);

        var nextQuery = _db.StkDeviceTasks.Where(x =>
            x.Status == StkDeviceTaskStatus.Created
            && x.LegId == completed.LegId);
        if (completed.PutAwayTaskId is Guid pa)
            nextQuery = nextQuery.Where(x => x.PutAwayTaskId == pa);
        else if (completed.RetrievalTaskId is Guid rt)
            nextQuery = nextQuery.Where(x => x.RetrievalTaskId == rt);

        var next = await nextQuery.OrderBy(x => x.Seq).FirstOrDefaultAsync(ct);
        if (next == null)
            return false;

        next.Status = StkDeviceTaskStatus.Dispatched;
        next.ModifyDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(next.ContainerCode, next.DestinationPointCode, next.LegId), ct);
        return true;
    }

    public async Task ReleaseFlowsAsync(Guid deviceTaskId, CancellationToken ct = default)
    {
        var flows = await _db.StkRouteFlows.Where(x => x.DeviceTaskId == deviceTaskId).ToListAsync(ct);
        if (flows.Count == 0) return;
        _db.StkRouteFlows.RemoveRange(flows);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<StkDeviceTask>> DispatchSingleAsync(
        StackerPathDispatchRequest request,
        string from,
        string to,
        CancellationToken ct)
    {
        var task = new StkDeviceTask
        {
            Id = Guid.NewGuid(),
            PutAwayTaskId = request.PutAwayTaskId,
            RetrievalTaskId = request.RetrievalTaskId,
            LegId = request.LegId,
            ContainerCode = request.ContainerCode,
            FromPointCode = from,
            DestinationPointCode = to,
            ExeStackCode = string.Empty,
            Seq = 1,
            Status = StkDeviceTaskStatus.Dispatched,
            CreateDate = DateTime.UtcNow
        };
        _db.StkDeviceTasks.Add(task);
        await _db.SaveChangesAsync(ct);
        await _port.DispatchDestinationAsync(
            new DispatchDestinationCommand(task.ContainerCode, task.DestinationPointCode, task.LegId), ct);
        return [task];
    }

    private async Task<string> ResolvePointAsync(string code, CancellationToken ct)
    {
        var trimmed = code.Trim();
        var coder = await _db.StkDeviceCoders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LocationCode == trimmed, ct);
        return coder?.PointCode ?? trimmed;
    }

    private static List<PathSegment> MergeSegments(IReadOnlyList<StkRoute> ordered)
    {
        var result = new List<PathSegment>();
        if (ordered.Count == 0) return result;

        var curFrom = ordered[0].FromCode;
        var curTo = ordered[0].ToCode;
        var curExe = ordered[0].ExeStackCode ?? string.Empty;
        var ids = new List<int> { ordered[0].Id };

        for (var i = 1; i < ordered.Count; i++)
        {
            var edge = ordered[i];
            var exe = edge.ExeStackCode ?? string.Empty;
            if (string.Equals(exe, curExe, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(exe))
            {
                curTo = edge.ToCode;
                ids.Add(edge.Id);
            }
            else
            {
                result.Add(new PathSegment(curFrom, curTo, curExe, ids.ToList()));
                curFrom = edge.FromCode;
                curTo = edge.ToCode;
                curExe = exe;
                ids = [edge.Id];
            }
        }

        result.Add(new PathSegment(curFrom, curTo, curExe, ids));
        return result;
    }

    private sealed record PathSegment(string From, string To, string ExeStackCode, List<int> RouteIds);
}
