using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Wcs.External;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Wcs.External;

public class ExtSystemConfiguration : IEntityTypeConfiguration<ExtSystem>
{
    public void Configure(EntityTypeBuilder<ExtSystem> builder)
    {
        builder.ToTable(TablePrefixes.External + "System");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PackId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Codec).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BaseUrl).HasMaxLength(512);
        builder.HasIndex(x => x.PackId).IsUnique();
    }
}

public class ExtMessageLogConfiguration : IEntityTypeConfiguration<ExtMessageLog>
{
    public void Configure(EntityTypeBuilder<ExtMessageLog> builder)
    {
        builder.ToTable(TablePrefixes.External + "MessageLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SystemPackId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.HasIndex(x => x.SystemPackId);
        builder.HasIndex(x => x.CorrelationId);
    }
}
