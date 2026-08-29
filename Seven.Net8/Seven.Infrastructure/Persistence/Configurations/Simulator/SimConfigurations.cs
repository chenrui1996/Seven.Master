using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seven.Domain.Entities.Simulator;
using Seven.Domain.Wcs;

namespace Seven.Infrastructure.Persistence.Configurations.Simulator;

public class SimDeploymentConfiguration : IEntityTypeConfiguration<SimDeployment>
{
    public void Configure(EntityTypeBuilder<SimDeployment> builder)
    {
        builder.ToTable(TablePrefixes.Simulator + "Deployment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProjectName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PackId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.WarehouseCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.ProjectName, x.Status });
    }
}
