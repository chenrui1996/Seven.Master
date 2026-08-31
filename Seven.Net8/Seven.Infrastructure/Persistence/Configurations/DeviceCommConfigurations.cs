using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations;

public class CommConnectionConfiguration : IEntityTypeConfiguration<CommConnection>
{
    public void Configure(EntityTypeBuilder<CommConnection> builder)
    {
        builder.ToTable(TablePrefixes.DeviceComm + "CommConnection");
        builder.HasKey(x => x.CommConnectionId);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CpuType).HasMaxLength(32);
        builder.Property(x => x.Remark).HasMaxLength(512);
        builder.HasIndex(x => x.Name);
    }
}

public class CommPointConfiguration : IEntityTypeConfiguration<CommPoint>
{
    public void Configure(EntityTypeBuilder<CommPoint> builder)
    {
        builder.ToTable(TablePrefixes.DeviceComm + "CommPoint");
        builder.HasKey(x => x.CommPointId);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Remark).HasMaxLength(512);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.CommConnectionId);
        builder.HasOne<CommConnection>()
            .WithMany()
            .HasForeignKey(x => x.CommConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommRuleConfiguration : IEntityTypeConfiguration<CommRule>
{
    public void Configure(EntityTypeBuilder<CommRule> builder)
    {
        builder.ToTable(TablePrefixes.DeviceComm + "CommRule");
        builder.HasKey(x => x.CommRuleId);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DefinitionJson).IsRequired();
        builder.Property(x => x.EventName).HasMaxLength(128);
        builder.Property(x => x.Remark).HasMaxLength(512);
        builder.HasIndex(x => x.CommConnectionId);
    }
}

public class CommEventLogConfiguration : IEntityTypeConfiguration<CommEventLog>
{
    public void Configure(EntityTypeBuilder<CommEventLog> builder)
    {
        builder.ToTable(TablePrefixes.DeviceComm + "CommEventLog");
        builder.HasKey(x => x.CommEventLogId);
        builder.Property(x => x.EventName).HasMaxLength(128);
        builder.Property(x => x.Message).HasMaxLength(512);
        builder.HasIndex(x => x.CommRuleId);
        builder.HasIndex(x => x.CreateDate);
    }
}
