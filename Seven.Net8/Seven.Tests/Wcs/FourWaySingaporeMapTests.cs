using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Enums;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Wcs.Packs.FourWay;
using Seven.Infrastructure.Wcs.Triggers;

namespace Seven.Tests.Wcs;

/// <summary>
/// 新加坡 fixture 灌图：Dijkstra / 选图 / 占边冲突（对齐 TC-FW-SG-001~003）。
/// </summary>
public class FourWaySingaporeMapTests
{
    [Fact]
    public async Task Dijkstra_L01_MainTrack_ShouldFindPathAlongRW1()
    {
        var db = CreateDb();
        var map = await SeedSingaporeLayerAsync(db, "Fw.L01");
        var from = "RW1_1Z001020601";
        var to = "RW1_1Z001060601";

        var edges = await db.FwRoutes.AsNoTracking()
            .Where(x => x.MapVersionId == map.Id)
            .Select(x => new FourWayEdge(x.FromCode, x.ToCode, x.Weight, x.Id.ToString()))
            .ToListAsync();

        var path = FourWayRouter.FindPath(from, to, edges);
        path.Should().NotBeEmpty();
        path[0].Should().Be(from);
        path[^1].Should().Be(to);
        path.Should().Contain("RW1_1Z001040601");
    }

    [Fact]
    public async Task ResolveMap_ShouldUseLayerCode_NotCrossLayerRoutes()
    {
        var db = CreateDb();
        var l01 = await SeedSingaporeLayerAsync(db, "Fw.L01");
        var l02 = await SeedSingaporeLayerAsync(db, "Fw.L02");
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var dispatcher = new FourWayPathDispatcher(db, port, guard);

        var shuttleId = Guid.NewGuid();
        var legId = Guid.NewGuid();
        db.FwShuttleTasks.Add(new FwShuttleTask
        {
            Id = shuttleId,
            LegId = legId,
            ContainerCode = "TP-SG-LAYER",
            FromCode = "RW1_1Z001020601",
            ToCode = "RW1_1Z001060601",
            Status = FwShuttleTaskStatus.Accepted,
            CreateDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var paths = await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(
            "RW1_1Z001020601",
            "RW1_1Z001060601",
            "TP-SG-LAYER",
            legId,
            shuttleId,
            MapVersionId: null,
            LayerCode: "Fw.L01"));

        paths.Should().NotBeEmpty();
        var edgeIds = paths.Where(p => p.EdgeId != null).Select(p => int.Parse(p.EdgeId!)).ToHashSet();
        var routes = await db.FwRoutes.Where(r => edgeIds.Contains(r.Id)).ToListAsync();
        routes.Should().OnlyContain(r => r.MapVersionId == l01.Id);
        routes.Should().NotContain(r => r.MapVersionId == l02.Id);
    }

    [Fact]
    public async Task Traffic_SameEdge_SecondStaysRouting_UntilRelease()
    {
        var db = CreateDb();
        var map = await SeedSingaporeLayerAsync(db, "Fw.L01");
        var port = new InMemoryEquipmentTriggerPort();
        var guard = new FourWayTrafficGuard();
        var dispatcher = new FourWayPathDispatcher(db, port, guard);

        var from = "RW1_1Z001020601";
        var to = "RW1_1Z001040601";

        var s1 = Guid.NewGuid();
        var l1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        db.FwShuttleTasks.AddRange(
            new FwShuttleTask
            {
                Id = s1,
                LegId = l1,
                ContainerCode = "TP-SG-1",
                FromCode = from,
                ToCode = to,
                Status = FwShuttleTaskStatus.Accepted,
                CreateDate = DateTime.UtcNow
            },
            new FwShuttleTask
            {
                Id = s2,
                LegId = l2,
                ContainerCode = "TP-SG-2",
                FromCode = from,
                ToCode = to,
                Status = FwShuttleTaskStatus.Accepted,
                CreateDate = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(from, to, "TP-SG-1", l1, s1, map.Id));
        await dispatcher.DispatchAsync(new FourWayPathDispatchRequest(from, to, "TP-SG-2", l2, s2, map.Id));

        (await db.FwShuttleTasks.SingleAsync(x => x.Id == s1)).Status.Should().Be(FwShuttleTaskStatus.Running);
        (await db.FwShuttleTasks.SingleAsync(x => x.Id == s2)).Status.Should().Be(FwShuttleTaskStatus.Routing);

        var firstPath = await db.FwShuttleTaskPaths
            .Where(x => x.ShuttleTaskId == s1 && x.EdgeId != null)
            .OrderBy(x => x.Seq)
            .FirstAsync();
        await guard.ReleaseAsync(firstPath.EdgeId!, s1.ToString("N"));

        (await dispatcher.RetryStuckRoutingAsync()).Should().BeGreaterThan(0);
        (await db.FwShuttleTasks.SingleAsync(x => x.Id == s2)).Status.Should().Be(FwShuttleTaskStatus.Running);
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"FwSg_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<FwMapVersion> SeedSingaporeLayerAsync(SevenDbContext db, string layerCode)
    {
        var fixture = LoadFixture();
        var mapJson = fixture.Maps.First(m => string.Equals(m.LayerCode, layerCode, StringComparison.OrdinalIgnoreCase));
        var map = new FwMapVersion
        {
            Code = mapJson.Code,
            Name = mapJson.Code,
            LayerCode = layerCode,
            IsActive = true,
            CreateDate = DateTime.UtcNow
        };
        db.FwMapVersions.Add(map);
        await db.SaveChangesAsync();

        foreach (var n in mapJson.Nodes)
        {
            db.FwNodes.Add(new FwNode
            {
                MapVersionId = map.Id,
                Code = n.Code,
                CreateDate = DateTime.UtcNow
            });
        }

        foreach (var r in mapJson.Routes)
        {
            db.FwRoutes.Add(new FwRoute
            {
                MapVersionId = map.Id,
                FromCode = r.FromCode,
                ToCode = r.ToCode,
                Weight = r.Weight <= 0 ? 1 : r.Weight,
                Capacity = r.Capacity <= 0 ? 1 : r.Capacity,
                CreateDate = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return map;
    }

    private static SingaporeFixture LoadFixture()
    {
        var path = ResolveFixturePath();
        File.Exists(path).Should().BeTrue($"missing fixture: {path}");
        var json = File.ReadAllText(path);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<SingaporeFixture>(json, opts)
               ?? throw new InvalidOperationException("singapore fixture empty");
    }

    private static string ResolveFixturePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "fixtures", "singapore-fourway-map.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "testplan", "fixtures", "singapore-fourway-map.json")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "testplan", "fixtures", "singapore-fourway-map.json")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "testplan", "fixtures", "singapore-fourway-map.json")),
        };
        return candidates.FirstOrDefault(File.Exists)
               ?? candidates[0];
    }

    private sealed class SingaporeFixture
    {
        public List<SingaporeMap> Maps { get; set; } = [];
    }

    private sealed class SingaporeMap
    {
        public string Code { get; set; } = "";
        public string LayerCode { get; set; } = "";
        public List<SingaporeNode> Nodes { get; set; } = [];
        public List<SingaporeRoute> Routes { get; set; } = [];
    }

    private sealed class SingaporeNode
    {
        public string Code { get; set; } = "";
    }

    private sealed class SingaporeRoute
    {
        public string FromCode { get; set; } = "";
        public string ToCode { get; set; } = "";
        public double Weight { get; set; }
        public int Capacity { get; set; }
    }
}
