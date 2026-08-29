using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Bus;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Bus;

public class BusTransportOrderConfiguration : IEntityTypeConfiguration<BusTransportOrder>
{
    public void Configure(EntityTypeBuilder<BusTransportOrder> builder)
    {
        builder.ToTable(TablePrefixes.Bus + "TransportOrder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromLocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToLocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FailReason).HasMaxLength(512);
        builder.Property(x => x.RefType).HasMaxLength(64);
        builder.Property(x => x.RefId).HasMaxLength(64);
        builder.HasMany(x => x.Legs)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Status);
    }
}

public class BusTransportLegConfiguration : IEntityTypeConfiguration<BusTransportLeg>
{
    public void Configure(EntityTypeBuilder<BusTransportLeg> builder)
    {
        builder.ToTable(TablePrefixes.Bus + "TransportLeg");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PackId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FromCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContainerCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.HandoverIn).HasMaxLength(64);
        builder.Property(x => x.HandoverOut).HasMaxLength(64);
        builder.Property(x => x.Message).HasMaxLength(512);
        builder.HasIndex(x => new { x.OrderId, x.Seq }).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
