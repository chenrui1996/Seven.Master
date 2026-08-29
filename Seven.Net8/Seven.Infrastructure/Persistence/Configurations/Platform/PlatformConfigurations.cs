using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Platform;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Platform;

public class IfcApiLogConfiguration : IEntityTypeConfiguration<IfcApiLog>
{
    public void Configure(EntityTypeBuilder<IfcApiLog> builder)
    {
        builder.ToTable(TablePrefixes.InterfaceLog + "ApiLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.OrderNo).HasMaxLength(64);
        builder.Property(x => x.Path).HasMaxLength(512);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => x.CreateDate);
        builder.HasIndex(x => x.SystemCode);
        builder.HasIndex(x => x.LegId);
    }
}

public class CtlModeConfiguration : IEntityTypeConfiguration<CtlMode>
{
    public void Configure(EntityTypeBuilder<CtlMode> builder)
    {
        builder.ToTable(TablePrefixes.Control + "Mode");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Scope).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Scope).IsUnique();
    }
}
