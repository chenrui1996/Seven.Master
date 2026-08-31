using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Platform;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Platform;

public class ScdViewConfiguration : IEntityTypeConfiguration<ScdView>
{
    public void Configure(EntityTypeBuilder<ScdView> builder)
    {
        builder.ToTable(TablePrefixes.Scada + "View");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class ScdNodeBindConfiguration : IEntityTypeConfiguration<ScdNodeBind>
{
    public void Configure(EntityTypeBuilder<ScdNodeBind> builder)
    {
        builder.ToTable(TablePrefixes.Scada + "NodeBind");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(128);
        builder.HasIndex(x => new { x.ViewId, x.LocationCode }).IsUnique();
        builder.HasOne<ScdView>().WithMany().HasForeignKey(x => x.ViewId).OnDelete(DeleteBehavior.Cascade);
    }
}
