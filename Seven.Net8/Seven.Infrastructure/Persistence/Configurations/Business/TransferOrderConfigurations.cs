using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Business;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Business;

public class TransferOrderConfiguration : IEntityTypeConfiguration<TransferOrder>
{
    public void Configure(EntityTypeBuilder<TransferOrder> builder)
    {
        builder.ToTable(TablePrefixes.Biz + "TransferOrder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Remark).HasMaxLength(512);
        builder.HasIndex(x => x.OrderNo).IsUnique();
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TransferOrderLineConfiguration : IEntityTypeConfiguration<TransferOrderLine>
{
    public void Configure(EntityTypeBuilder<TransferOrderLine> builder)
    {
        builder.ToTable(TablePrefixes.Biz + "TransferOrderLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Qty).HasPrecision(18, 4);
        builder.Property(x => x.CompletedQty).HasPrecision(18, 4);
        builder.Property(x => x.FromLocation).HasMaxLength(64);
        builder.Property(x => x.ToLocation).HasMaxLength(64);
        builder.Property(x => x.ContainerCode).HasMaxLength(64);
        builder.HasIndex(x => new { x.OrderId, x.LineNo }).IsUnique();
    }
}
