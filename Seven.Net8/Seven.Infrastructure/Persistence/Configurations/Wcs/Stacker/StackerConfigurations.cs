using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Wcs.Stacker;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Wcs.Stacker;

public class StkRequestPointConfiguration : IEntityTypeConfiguration<StkRequestPoint>
{
    public void Configure(EntityTypeBuilder<StkRequestPoint> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "RequestPoint");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AisleCode).HasMaxLength(32);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class StkAssignmentPolicyConfiguration : IEntityTypeConfiguration<StkAssignmentPolicy>
{
    public void Configure(EntityTypeBuilder<StkAssignmentPolicy> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "AssignmentPolicy");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AisleCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DestinationPointCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MinEmptySlots).HasDefaultValue(0);
        builder.Property(x => x.AllocationWeight).HasDefaultValue(1);
        builder.HasIndex(x => x.AisleCode).IsUnique();
    }
}

public class StkAssignmentRecordConfiguration : IEntityTypeConfiguration<StkAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<StkAssignmentRecord> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "AssignmentRecord");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AisleCode).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.AisleCode).IsUnique();
    }
}

public class StkPutAwayTaskConfiguration : IEntityTypeConfiguration<StkPutAwayTask>
{
    public void Configure(EntityTypeBuilder<StkPutAwayTask> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "PutAwayTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AssignedAisle).HasMaxLength(32);
        builder.Property(x => x.AssignedLocationCode).HasMaxLength(64);
        builder.HasIndex(x => x.LegId).IsUnique();
        builder.HasIndex(x => x.ContainerCode);
        builder.HasIndex(x => x.Status);
    }
}

public class StkRetrievalTaskConfiguration : IEntityTypeConfiguration<StkRetrievalTask>
{
    public void Configure(EntityTypeBuilder<StkRetrievalTask> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "RetrievalTask");
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

public class StkDeviceTaskConfiguration : IEntityTypeConfiguration<StkDeviceTask>
{
    public void Configure(EntityTypeBuilder<StkDeviceTask> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "DeviceTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromPointCode).HasMaxLength(64).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(x => x.DestinationPointCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExeStackCode).HasMaxLength(32).IsRequired().HasDefaultValue(string.Empty);
        builder.HasIndex(x => x.LegId);
        builder.HasIndex(x => x.PutAwayTaskId);
        builder.HasIndex(x => x.RetrievalTaskId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.LegId, x.Seq });
    }
}

public class StkRouteConfiguration : IEntityTypeConfiguration<StkRoute>
{
    public void Configure(EntityTypeBuilder<StkRoute> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "Route");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MapCode).HasMaxLength(32).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExeStackCode).HasMaxLength(32).IsRequired().HasDefaultValue(string.Empty);
        builder.HasIndex(x => new { x.MapCode, x.FromCode, x.ToCode });
        builder.HasIndex(x => x.IsEnabled);
    }
}

public class StkRouteFlowConfiguration : IEntityTypeConfiguration<StkRouteFlow>
{
    public void Configure(EntityTypeBuilder<StkRouteFlow> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "RouteFlow");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.RouteId);
        builder.HasIndex(x => x.DeviceTaskId);
        builder.HasIndex(x => x.LegId);
    }
}

public class StkDeviceCoderConfiguration : IEntityTypeConfiguration<StkDeviceCoder>
{
    public void Configure(EntityTypeBuilder<StkDeviceCoder> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "DeviceCoder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PointCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.LocationCode).IsUnique();
    }
}

public class StkLocationProfileConfiguration : IEntityTypeConfiguration<StkLocationProfile>
{
    public void Configure(EntityTypeBuilder<StkLocationProfile> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "LocationProfile");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BinGroupCode).HasMaxLength(64);
        builder.HasIndex(x => x.LocationCode).IsUnique();
        builder.HasIndex(x => x.BinGroupCode);
    }
}
