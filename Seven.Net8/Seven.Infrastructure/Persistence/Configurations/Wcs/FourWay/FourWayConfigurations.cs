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
        builder.HasIndex(x => x.Code).IsUnique();
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
