using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Wms;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Wms;

public class WmsWarehouseConfiguration : IEntityTypeConfiguration<WmsWarehouse>
{
    public void Configure(EntityTypeBuilder<WmsWarehouse> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "Warehouse");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class WmsZoneConfiguration : IEntityTypeConfiguration<WmsZone>
{
    public void Configure(EntityTypeBuilder<WmsZone> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "Zone");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();
        builder.HasOne<WmsWarehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WmsLocationConfiguration : IEntityTypeConfiguration<WmsLocation>
{
    public void Configure(EntityTypeBuilder<WmsLocation> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "Location");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Aisle).HasMaxLength(32);
        builder.Property(x => x.Row).HasMaxLength(32);
        builder.Property(x => x.Column).HasMaxLength(32);
        builder.Property(x => x.Layer).HasMaxLength(32);
        builder.Property(x => x.CurrentContainerCode).HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.WarehouseId);
        builder.HasOne<WmsWarehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WmsZone>()
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WmsContainerTypeConfiguration : IEntityTypeConfiguration<WmsContainerType>
{
    public void Configure(EntityTypeBuilder<WmsContainerType> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "ContainerType");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class WmsContainerConfiguration : IEntityTypeConfiguration<WmsContainer>
{
    public void Configure(EntityTypeBuilder<WmsContainer> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "Container");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LocationCode).HasMaxLength(64);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasOne<WmsContainerType>()
            .WithMany()
            .HasForeignKey(x => x.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WmsStockConfiguration : IEntityTypeConfiguration<WmsStock>
{
    public void Configure(EntityTypeBuilder<WmsStock> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "Stock");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContainerCode).HasMaxLength(64);
        builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Qty).HasPrecision(18, 4);
        builder.Property(x => x.AvailableQty).HasPrecision(18, 4);
        builder.Property(x => x.Lot).HasMaxLength(64);
        builder.HasIndex(x => new { x.LocationCode, x.MaterialCode, x.ContainerCode, x.Lot });
    }
}

public class WmsStockLedgerConfiguration : IEntityTypeConfiguration<WmsStockLedger>
{
    public void Configure(EntityTypeBuilder<WmsStockLedger> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "StockLedger");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContainerCode).HasMaxLength(64);
        builder.Property(x => x.DeltaQty).HasPrecision(18, 4);
        builder.Property(x => x.Reason).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RefType).HasMaxLength(64);
        builder.Property(x => x.RefId).HasMaxLength(64);
        builder.HasIndex(x => x.StockId);
        builder.HasIndex(x => x.CreateDate);
        builder.HasOne(x => x.Stock)
            .WithMany()
            .HasForeignKey(x => x.StockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WmsInboundOrderConfiguration : IEntityTypeConfiguration<WmsInboundOrder>
{
    public void Configure(EntityTypeBuilder<WmsInboundOrder> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "InboundOrder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.OrderNo).IsUnique();
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WmsInboundOrderLineConfiguration : IEntityTypeConfiguration<WmsInboundOrderLine>
{
    public void Configure(EntityTypeBuilder<WmsInboundOrderLine> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "InboundOrderLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Qty).HasPrecision(18, 4);
        builder.Property(x => x.CompletedQty).HasPrecision(18, 4);
        builder.Property(x => x.ContainerCode).HasMaxLength(64);
        builder.Property(x => x.FromLocation).HasMaxLength(64);
        builder.Property(x => x.ToLocation).HasMaxLength(64);
        builder.HasIndex(x => new { x.OrderId, x.LineNo }).IsUnique();
    }
}

public class WmsOutboundOrderConfiguration : IEntityTypeConfiguration<WmsOutboundOrder>
{
    public void Configure(EntityTypeBuilder<WmsOutboundOrder> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "OutboundOrder");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.OrderNo).IsUnique();
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WmsOutboundOrderLineConfiguration : IEntityTypeConfiguration<WmsOutboundOrderLine>
{
    public void Configure(EntityTypeBuilder<WmsOutboundOrderLine> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "OutboundOrderLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Qty).HasPrecision(18, 4);
        builder.Property(x => x.CompletedQty).HasPrecision(18, 4);
        builder.Property(x => x.ContainerCode).HasMaxLength(64);
        builder.Property(x => x.FromLocation).HasMaxLength(64);
        builder.Property(x => x.ToLocation).HasMaxLength(64);
        builder.HasIndex(x => new { x.OrderId, x.LineNo }).IsUnique();
    }
}

public class WmsCycleCountConfiguration : IEntityTypeConfiguration<WmsCycleCount>
{
    public void Configure(EntityTypeBuilder<WmsCycleCount> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "CycleCount");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.OrderNo).IsUnique();
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WmsCycleCountLineConfiguration : IEntityTypeConfiguration<WmsCycleCountLine>
{
    public void Configure(EntityTypeBuilder<WmsCycleCountLine> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "CycleCountLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MaterialCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContainerCode).HasMaxLength(64);
        builder.Property(x => x.BookQty).HasPrecision(18, 4);
        builder.Property(x => x.CountQty).HasPrecision(18, 4);
        builder.Property(x => x.DiffQty).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.OrderId, x.LineNo }).IsUnique();
    }
}

public class WmsHandoverLinkConfiguration : IEntityTypeConfiguration<WmsHandoverLink>
{
    public void Configure(EntityTypeBuilder<WmsHandoverLink> builder)
    {
        builder.ToTable(TablePrefixes.Wms + "HandoverLink");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromPackId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ToPackId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LocationCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.FromPackId, x.ToPackId, x.LocationCode }).IsUnique();
    }
}
