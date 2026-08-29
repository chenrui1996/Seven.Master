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

public class StkDeviceTaskConfiguration : IEntityTypeConfiguration<StkDeviceTask>
{
    public void Configure(EntityTypeBuilder<StkDeviceTask> builder)
    {
        builder.ToTable(TablePrefixes.Stacker + "DeviceTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DestinationPointCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.LegId);
        builder.HasIndex(x => x.PutAwayTaskId);
        builder.HasIndex(x => x.Status);
    }
}
