using Seven.Domain.Common;

namespace Seven.Domain.Entities.Simulator;

/// <summary>仿真部署记录（工程 Deploy 结果）。</summary>
public class SimDeployment : BaseEntity
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string PackId { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Deployed"; // Deployed | Undeployed
    public int LocationCount { get; set; }
    public int EdgeCount { get; set; }
    public string? ProjectJson { get; set; }
    public DateTime? DeployedAt { get; set; }
    public DateTime? UndeployedAt { get; set; }
}
