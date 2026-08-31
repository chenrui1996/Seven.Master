using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Seven.Domain.Entities.Platform;
using Seven.Domain.Entities.Wms;
using Seven.Infrastructure.Persistence;
using Seven.Infrastructure.Scada;

namespace Seven.Tests.Scada;

public class Scada2dTests
{
    [Fact]
    public async Task CreateViewAndBind_ListByViewId_ShouldReturnCoordinates()
    {
        var db = CreateDb();
        var viewService = new ScadaViewService(db);
        var bindService = new ScadaNodeBindService(db);

        var view = new ScdView { Code = "FLOOR-1", Name = "一楼", Width = 800, Height = 600 };
        (await viewService.AddAsync(view)).Status.Should().BeTrue();
        view.Id.Should().BeGreaterThan(0);

        var bind = new ScdNodeBind
        {
            ViewId = view.Id,
            LocationCode = "LOC-A1-01",
            X = 120.5,
            Y = 340.0,
            Label = "A1-01",
        };
        (await bindService.AddAsync(bind)).Status.Should().BeTrue();

        var list = await bindService.ListByViewIdAsync(view.Id);
        list.Should().HaveCount(1);
        list[0].X.Should().Be(120.5);
        list[0].Y.Should().Be(340.0);
        list[0].LocationCode.Should().Be("LOC-A1-01");
    }

    [Fact]
    public async Task GetStatus_ShouldJoinWmsLocationIsOccupied()
    {
        var db = CreateDb();
        var viewService = new ScadaViewService(db);
        var bindService = new ScadaNodeBindService(db);

        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = 1,
            Code = "LOC-A1-01",
            IsOccupied = true,
            IsDeleted = false,
        });
        db.WmsLocations.Add(new WmsLocation
        {
            WarehouseId = 1,
            Code = "LOC-A1-02",
            IsOccupied = false,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var view = new ScdView { Code = "FLOOR-2", Name = "二楼", Width = 1024, Height = 768 };
        await viewService.AddAsync(view);

        await bindService.AddAsync(new ScdNodeBind { ViewId = view.Id, LocationCode = "LOC-A1-01", X = 10, Y = 20 });
        await bindService.AddAsync(new ScdNodeBind { ViewId = view.Id, LocationCode = "LOC-A1-02", X = 30, Y = 40 });

        var status = await viewService.GetStatusAsync(view.Id);
        status.Should().NotBeNull();
        status!.Width.Should().Be(1024);
        status.Nodes.Should().HaveCount(2);
        status.Nodes.Single(n => n.LocationCode == "LOC-A1-01").IsOccupied.Should().BeTrue();
        status.Nodes.Single(n => n.LocationCode == "LOC-A1-02").IsOccupied.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatus_UnknownLocationCode_ShouldDefaultNotOccupied()
    {
        var db = CreateDb();
        var viewService = new ScadaViewService(db);
        var bindService = new ScadaNodeBindService(db);

        var view = new ScdView { Code = "FLOOR-3", Name = "三楼", Width = 640, Height = 480 };
        await viewService.AddAsync(view);
        await bindService.AddAsync(new ScdNodeBind { ViewId = view.Id, LocationCode = "UNKNOWN", X = 5, Y = 5 });

        var status = await viewService.GetStatusAsync(view.Id);
        status!.Nodes[0].IsOccupied.Should().BeFalse();
    }

    private static SevenDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SevenDbContext>()
            .UseInMemoryDatabase($"Scada_{Guid.NewGuid():N}")
            .Options;
        var db = new SevenDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
