# Review package Task 1 (working tree — no commits)

## Files (Task 1 scope)
Seven.Net8\Seven.Domain\Entities\Wcs\FourWay\FwAislePolicy.cs
Seven.Net8\Seven.Domain\Entities\Wcs\FourWay\FwAssignmentRecord.cs
Seven.Net8\Seven.Domain\Entities\Wcs\FourWay\FwPutAwayTask.cs
Seven.Net8\Seven.Domain\Entities\Wcs\FourWay\FwRequestPoint.cs
Seven.Net8\Seven.Domain\Enums\FourWayEnums.cs
Seven.Net8\Seven.Infrastructure\Persistence\Migrations\20260829114832_AddFwPolicyPutAwayRequest.cs
Seven.Net8\Seven.Infrastructure\Persistence\Migrations\20260829114832_AddFwPolicyPutAwayRequest.Designer.cs
 M Seven.Net8/Seven.Domain/Enums/FourWayEnums.cs
 M Seven.Net8/Seven.Infrastructure/Migrations/SevenDbContextModelSnapshot.cs
 M Seven.Net8/Seven.Infrastructure/Persistence/Configurations/Wcs/FourWay/FourWayConfigurations.cs
 M Seven.Net8/Seven.Infrastructure/Persistence/SevenDbContext.cs
?? Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwAislePolicy.cs
?? Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwAssignmentRecord.cs
?? Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwLayerPolicy.cs
?? Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwPutAwayTask.cs
?? Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwRequestPoint.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829111616_AddStkAssignmentPolicyEmptyWeight.Designer.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829111616_AddStkAssignmentPolicyEmptyWeight.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829112353_AddStkRouteFlowCoder.Designer.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829112353_AddStkRouteFlowCoder.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829113142_AddStkLocationProfileAndCycleLock.Designer.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829113142_AddStkLocationProfileAndCycleLock.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829114832_AddFwPolicyPutAwayRequest.Designer.cs
?? Seven.Net8/Seven.Infrastructure/Persistence/Migrations/20260829114832_AddFwPolicyPutAwayRequest.cs

## Diff (new entities + configs + migration excerpts via Get-Content)

### FILE: Seven.Net8/Seven.Domain/Enums/FourWayEnums.cs
```csharp
namespace Seven.Domain.Enums;

/// <summary>鍥涘悜杞︾┛姊换鍔＄姸鎬併€?/summary>
public enum FwShuttleTaskStatus
{
    Accepted = 0,
    Routing = 1,
    Running = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}

/// <summary>鍥涘悜鍏ュ簱涓婃灦浠诲姟鐘舵€併€?/summary>
public enum FwPutAwayStatus
{
    Accepted = 0,
    LayerAssigned = 1,
    AisleAssigned = 2,
    LocationAssigned = 3,
    Completed = 4,
    Cancelled = 5,
    Failed = 6
}

/// <summary>鍥涘悜鐩殑鍦扮敵璇风偣绫诲瀷銆?/summary>
public enum FwRequestPointType
{
    LayerRequest = 0,
    AisleRequest = 1,
    LocationRequest = 2
    // F5: Hoist* later
}

/// <summary>鍥涘悜鍒嗛厤杞浆璁板綍鑼冨洿锛堝眰 / 宸凤級銆?/summary>
public enum FwAssignmentScopeType
{
    Layer = 0,
    Aisle = 1
}
```

### FILE: Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwAislePolicy.cs
```csharp
using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>鍥涘悜宸峰垎閰嶇瓥鐣ワ紙瀵归綈 RCS AssignmentPolicy.AisleCode锛夈€?/summary>
public class FwAislePolicy : BaseEntity
{
    public int Id { get; set; }
    public string LayerCode { get; set; } = string.Empty;
    public string AisleCode { get; set; } = string.Empty;
    /// <summary>宸烽亾鍐呰嚦灏戠┖闂茶揣浣嶆暟锛?=涓嶆牎楠岋級銆?/summary>
    public int MinEmptySlots { get; set; }
    public int MaxShuttleCount { get; set; }
    public string DestinationPointCode { get; set; } = string.Empty;
    /// <summary>鍒嗛厤鏉冮噸锛岃秺澶ц秺浼樺厛锛堝悓鏉冮噸鍐嶆寜杞浆鏃堕棿锛夈€?/summary>
    public int AllocationWeight { get; set; } = 1;
    public bool IsAvailable { get; set; } = true;
    public int MaxHeight { get; set; } = 9999;
    public decimal MaxWeight { get; set; } = 99999;
}
```

### FILE: Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwAssignmentRecord.cs
```csharp
using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>鍥涘悜灞?宸疯疆杞褰曪細鏈€杩戜竴娆″垎閰嶃€?/summary>
public class FwAssignmentRecord : BaseEntity
{
    public int Id { get; set; }
    public FwAssignmentScopeType ScopeType { get; set; }
    public string ScopeCode { get; set; } = string.Empty;
    public DateTime LastAssignedAt { get; set; }
    public int AssignCount { get; set; }
}
```

### FILE: Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwPutAwayTask.cs
```csharp
using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>鍥涘悜鍏ュ簱涓婃灦浠诲姟锛?:1 瀵瑰簲鎬荤嚎 Leg銆?/summary>
public class FwPutAwayTask : BaseEntity
{
    public Guid Id { get; set; }
    public Guid LegId { get; set; }
    public string ContainerCode { get; set; } = string.Empty;
    public string FromCode { get; set; } = string.Empty;
    public string ToCode { get; set; } = string.Empty;
    public FwPutAwayStatus Status { get; set; }
    public string? AssignedLayer { get; set; }
    public string? AssignedAisle { get; set; }
    public string? AssignedLocationCode { get; set; }
}
```

### FILE: Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwRequestPoint.cs
```csharp
using Seven.Domain.Common;
using Seven.Domain.Enums;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>鍥涘悜鐩殑鍦扮敵璇风偣锛圫UDR 璇箟鍏ュ彛锛夈€?/summary>
public class FwRequestPoint : BaseEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public FwRequestPointType PointType { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? LayerCode { get; set; }
    public string? AisleCode { get; set; }
}
```

### FILE: Seven.Net8/Seven.Domain/Entities/Wcs/FourWay/FwLayerPolicy.cs
```csharp
using Seven.Domain.Common;

namespace Seven.Domain.Entities.Wcs.FourWay;

/// <summary>鍥涘悜灞傚垎閰嶇瓥鐣ワ紙瀵归綈 RCS AssignmentPolicy.LayerCode锛夈€?/summary>
public class FwLayerPolicy : BaseEntity
{
    public int Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string LayerCode { get; set; } = string.Empty;
    public int MaxHeight { get; set; } = 9999;
    public decimal MaxWeight { get; set; } = 99999;
    public bool IsAvailable { get; set; } = true;
    /// <summary>鍒嗛厤鏉冮噸锛岃秺澶ц秺浼樺厛锛堝悓鏉冮噸鍐嶆寜杞浆鏃堕棿锛夈€?/summary>
    public int AllocationWeight { get; set; } = 1;
    public bool StartSign { get; set; }
    public int NextPolicyId { get; set; }
}
```

### FILE: D:\Junheinrich\Junheinrich.Master\Seven.Master\Seven.Net8\Seven.Infrastructure\Persistence\Migrations\20260829114832_AddFwPolicyPutAwayRequest.cs
```csharp
using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFwPolicyPutAwayRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllocationWeight",
                table: "Fw_LayerPolicy",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Fw_AislePolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LayerCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AisleCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinEmptySlots = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxShuttleCount = table.Column<int>(type: "int", nullable: false),
                    DestinationPointCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AllocationWeight = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxHeight = table.Column<int>(type: "int", nullable: false),
                    MaxWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreateId = table.Column<int>(type: "int", nullable: true),
                    Creator = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifyId = table.Column<int>(type: "int", nullable: true),
                    Modifier = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifyDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fw_AislePolicy", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Fw_AssignmentRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ScopeType = table.Column<int>(type: "int", nullable: false),
                    ScopeCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastAssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AssignCount = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreateId = table.Column<int>(type: "int", nullable: true),
                    Creator = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifyId = table.Column<int>(type: "int", nullable: true),
                    Modifier = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifyDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fw_AssignmentRecord", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Fw_PutAwayTask",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LegId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContainerCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedLayer = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedAisle = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedLocationCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreateId = table.Column<int>(type: "int", nullable: true),
                    Creator = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifyId = table.Column<int>(type: "int", nullable: true),
                    Modifier = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifyDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fw_PutAwayTask", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Fw_RequestPoint",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PointType = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LayerCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AisleCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreateId = table.Column<int>(type: "int", nullable: true),
                    Creator = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModifyId = table.Column<int>(type: "int", nullable: true),
                    Modifier = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifyDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fw_RequestPoint", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Fw_AislePolicy_LayerCode_AisleCode",
                table: "Fw_AislePolicy",
                columns: new[] { "LayerCode", "AisleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fw_AssignmentRecord_ScopeType_ScopeCode",
                table: "Fw_AssignmentRecord",
                columns: new[] { "ScopeType", "ScopeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fw_PutAwayTask_ContainerCode",
                table: "Fw_PutAwayTask",
                column: "ContainerCode");

            migrationBuilder.CreateIndex(
                name: "IX_Fw_PutAwayTask_LegId",
                table: "Fw_PutAwayTask",
                column: "LegId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fw_PutAwayTask_Status",
                table: "Fw_PutAwayTask",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Fw_RequestPoint_Code",
                table: "Fw_RequestPoint",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fw_AislePolicy");

            migrationBuilder.DropTable(
                name: "Fw_AssignmentRecord");

            migrationBuilder.DropTable(
                name: "Fw_PutAwayTask");

            migrationBuilder.DropTable(
                name: "Fw_RequestPoint");

            migrationBuilder.DropColumn(
                name: "AllocationWeight",
                table: "Fw_LayerPolicy");
        }
    }
}
```
