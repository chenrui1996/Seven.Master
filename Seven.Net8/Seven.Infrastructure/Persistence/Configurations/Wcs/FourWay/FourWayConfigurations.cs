using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Wcs.FourWay;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Wcs.FourWay;

public class FwMapVersionConfiguration : IEntityTypeConfiguration<FwMapVersion>
{
    public void Configure(EntityTypeBuilder<FwMapVersion> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "MapVersion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LayerCode).HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.LayerCode);
    }
}

public class FwNodeConfiguration : IEntityTypeConfiguration<FwNode>
{
    public void Configure(EntityTypeBuilder<FwNode> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "Node");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LocationCode).HasMaxLength(64);
        builder.HasIndex(x => new { x.MapVersionId, x.Code }).IsUnique();
        builder.HasIndex(x => x.LocationCode);
    }
}

public class FwRouteGroupConfiguration : IEntityTypeConfiguration<FwRouteGroup>
{
    public void Configure(EntityTypeBuilder<FwRouteGroup> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "RouteGroup");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.MapVersionId, x.Code }).IsUnique();
    }
}

public class FwRouteConfiguration : IEntityTypeConfiguration<FwRoute>
{
    public void Configure(EntityTypeBuilder<FwRoute> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "Route");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.MapVersionId);
        builder.HasIndex(x => new { x.MapVersionId, x.FromNodeId, x.ToNodeId }).IsUnique();
    }
}

public class FwShuttleTaskConfiguration : IEntityTypeConfiguration<FwShuttleTask>
{
    public void Configure(EntityTypeBuilder<FwShuttleTask> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "ShuttleTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.LegId).IsUnique();
        builder.HasIndex(x => x.ContainerCode);
        builder.HasIndex(x => x.Status);
    }
}

public class FwShuttleTaskPathConfiguration : IEntityTypeConfiguration<FwShuttleTaskPath>
{
    public void Configure(EntityTypeBuilder<FwShuttleTaskPath> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "ShuttleTaskPath");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NodeCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EdgeId).HasMaxLength(64);
        builder.HasIndex(x => new { x.ShuttleTaskId, x.Seq }).IsUnique();
    }
}

public class FwLayerPolicyConfiguration : IEntityTypeConfiguration<FwLayerPolicy>
{
    public void Configure(EntityTypeBuilder<FwLayerPolicy> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "LayerPolicy");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WarehouseCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ZoneCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LayerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MaxWeight).HasPrecision(18, 4);
        builder.Property(x => x.AllocationWeight).HasDefaultValue(1);
        builder.HasIndex(x => new { x.WarehouseCode, x.ZoneCode, x.LayerCode });
    }
}

public class FwAislePolicyConfiguration : IEntityTypeConfiguration<FwAislePolicy>
{
    public void Configure(EntityTypeBuilder<FwAislePolicy> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "AislePolicy");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LayerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AisleCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DestinationPointCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MinEmptySlots).HasDefaultValue(0);
        builder.Property(x => x.AllocationWeight).HasDefaultValue(1);
        builder.Property(x => x.MaxWeight).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.LayerCode, x.AisleCode }).IsUnique();
    }
}

public class FwAssignmentRecordConfiguration : IEntityTypeConfiguration<FwAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<FwAssignmentRecord> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "AssignmentRecord");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScopeCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ScopeType, x.ScopeCode }).IsUnique();
    }
}

public class FwPutAwayTaskConfiguration : IEntityTypeConfiguration<FwPutAwayTask>
{
    public void Configure(EntityTypeBuilder<FwPutAwayTask> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "PutAwayTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AssignedLayer).HasMaxLength(64);
        builder.Property(x => x.AssignedAisle).HasMaxLength(64);
        builder.Property(x => x.AssignedLocationCode).HasMaxLength(64);
        builder.HasIndex(x => x.LegId).IsUnique();
        builder.HasIndex(x => x.ContainerCode);
        builder.HasIndex(x => x.Status);
    }
}

public class FwRequestPointConfiguration : IEntityTypeConfiguration<FwRequestPoint>
{
    public void Configure(EntityTypeBuilder<FwRequestPoint> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "RequestPoint");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LayerCode).HasMaxLength(64);
        builder.Property(x => x.AisleCode).HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class FwRetrievalTaskConfiguration : IEntityTypeConfiguration<FwRetrievalTask>
{
    public void Configure(EntityTypeBuilder<FwRetrievalTask> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "RetrievalTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.WcsGroupNo).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.LegId).IsUnique();
        builder.HasIndex(x => new { x.WcsGroupNo, x.WcsPri });
        builder.HasIndex(x => x.ContainerCode);
        builder.HasIndex(x => x.Status);
    }
}

public class FwParkingLedgerConfiguration : IEntityTypeConfiguration<FwParkingLedger>
{
    public void Configure(EntityTypeBuilder<FwParkingLedger> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "ParkingLedger");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LocationCode).HasMaxLength(64);
        builder.Property(x => x.LayerCode).HasMaxLength(64);
        builder.Property(x => x.AisleCode).HasMaxLength(64);
        builder.Property(x => x.ContainerCode).HasMaxLength(64);
        builder.Property(x => x.ShuttleNo).HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => new { x.LayerCode, x.AisleCode, x.Status });
    }
}

public class FwHoistDeviceConfiguration : IEntityTypeConfiguration<FwHoistDevice>
{
    public void Configure(EntityTypeBuilder<FwHoistDevice> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "HoistDevice");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HoistNo).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128);
        builder.Property(x => x.CurrentLayer).HasMaxLength(64);
        builder.Property(x => x.CurrentLocation).HasMaxLength(64);
        builder.HasIndex(x => x.HoistNo).IsUnique();
    }
}

public class FwHoistLayerPointConfiguration : IEntityTypeConfiguration<FwHoistLayerPoint>
{
    public void Configure(EntityTypeBuilder<FwHoistLayerPoint> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "HoistLayerPoint");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LayerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.HoistNo).HasMaxLength(64).IsRequired();
        builder.Property(x => x.InboundEp).HasMaxLength(64);
        builder.Property(x => x.InboundAp).HasMaxLength(64);
        builder.Property(x => x.OutboundEp).HasMaxLength(64);
        builder.Property(x => x.OutboundAp).HasMaxLength(64);
        builder.HasIndex(x => new { x.HoistNo, x.LayerCode }).IsUnique();
    }
}

public class FwHoistTaskConfiguration : IEntityTypeConfiguration<FwHoistTask>
{
    public void Configure(EntityTypeBuilder<FwHoistTask> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "HoistTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.HoistNo).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SrcLayer).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SrcAddress).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DesLayer).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DesAddress).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.LegId).IsUnique();
        builder.HasIndex(x => x.ContainerCode);
        builder.HasIndex(x => x.Status);
    }
}

public class FwHoistExecTaskConfiguration : IEntityTypeConfiguration<FwHoistExecTask>
{
    public void Configure(EntityTypeBuilder<FwHoistExecTask> builder)
    {
        builder.ToTable(TablePrefixes.FourWay + "HoistExecTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.HoistNo).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SrcLayer).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SrcAddress).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DesLayer).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DesAddress).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.HoistTaskId);
        builder.HasIndex(x => x.LegId);
        builder.HasIndex(x => new { x.HoistNo, x.SrcAddress, x.Status });
        builder.HasIndex(x => x.Status);
    }
}
