using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWmsLayerAislePackPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL：FK_Wms_Zone_Wms_Warehouse_WarehouseId / FK_Wms_Location_Wms_Warehouse_WarehouseId
            // 依赖 WarehouseId 上的索引。必须先建替代索引，再 Drop 旧索引，否则报
            // "Cannot drop index ... needed in a foreign key constraint"。

            migrationBuilder.AddColumn<string>(
                name: "PackId",
                table: "Wms_Zone",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EnabledPackIds",
                table: "Wms_Warehouse",
                type: "varchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Aisle",
                table: "Wms_Location",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AisleId",
                table: "Wms_Location",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Depth",
                table: "Wms_Location",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsBooked",
                table: "Wms_Location",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LayerId",
                table: "Wms_Location",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackId",
                table: "Wms_Location",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Fw_LayerPolicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WarehouseCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZoneCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LayerCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxHeight = table.Column<int>(type: "int", nullable: false),
                    MaxWeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StartSign = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    NextPolicyId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Fw_LayerPolicy", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Wms_Layer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    PackId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllocationWeight = table.Column<int>(type: "int", nullable: false),
                    MaxShuttleCount = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Wms_Layer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wms_Layer_Wms_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Wms_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wms_Layer_Wms_Zone_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Wms_Zone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Wms_Aisle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    LayerId = table.Column<int>(type: "int", nullable: true),
                    PackId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EpPointCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllocationWeight = table.Column<int>(type: "int", nullable: false),
                    MaxDepth = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Wms_Aisle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wms_Aisle_Wms_Layer_LayerId",
                        column: x => x.LayerId,
                        principalTable: "Wms_Layer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wms_Aisle_Wms_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Wms_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Wms_Aisle_Wms_Zone_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Wms_Zone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Zone_Code",
                table: "Wms_Zone",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Zone_WarehouseId_PackId",
                table: "Wms_Zone",
                columns: new[] { "WarehouseId", "PackId" });

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Location_AisleId",
                table: "Wms_Location",
                column: "AisleId");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Location_LayerId",
                table: "Wms_Location",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Location_WarehouseId_PackId",
                table: "Wms_Location",
                columns: new[] { "WarehouseId", "PackId" });

            // 替代索引已覆盖 WarehouseId 左前缀，此时可安全删除旧索引
            migrationBuilder.DropIndex(
                name: "IX_Wms_Zone_WarehouseId_Code",
                table: "Wms_Zone");

            migrationBuilder.DropIndex(
                name: "IX_Wms_Location_WarehouseId",
                table: "Wms_Location");

            migrationBuilder.CreateIndex(
                name: "IX_Fw_LayerPolicy_WarehouseCode_ZoneCode_LayerCode",
                table: "Fw_LayerPolicy",
                columns: new[] { "WarehouseCode", "ZoneCode", "LayerCode" });

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Aisle_Code",
                table: "Wms_Aisle",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Aisle_LayerId",
                table: "Wms_Aisle",
                column: "LayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Aisle_WarehouseId",
                table: "Wms_Aisle",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Aisle_ZoneId",
                table: "Wms_Aisle",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Layer_Code",
                table: "Wms_Layer",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Layer_WarehouseId",
                table: "Wms_Layer",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Layer_ZoneId",
                table: "Wms_Layer",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wms_Location_Wms_Aisle_AisleId",
                table: "Wms_Location",
                column: "AisleId",
                principalTable: "Wms_Aisle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Wms_Location_Wms_Layer_LayerId",
                table: "Wms_Location",
                column: "LayerId",
                principalTable: "Wms_Layer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wms_Location_Wms_Aisle_AisleId",
                table: "Wms_Location");

            migrationBuilder.DropForeignKey(
                name: "FK_Wms_Location_Wms_Layer_LayerId",
                table: "Wms_Location");

            migrationBuilder.DropTable(
                name: "Fw_LayerPolicy");

            migrationBuilder.DropTable(
                name: "Wms_Aisle");

            migrationBuilder.DropTable(
                name: "Wms_Layer");

            // 先建回旧索引（覆盖 WarehouseId），再删 PackId 复合索引，避免 MySQL FK 报错
            migrationBuilder.CreateIndex(
                name: "IX_Wms_Zone_WarehouseId_Code",
                table: "Wms_Zone",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wms_Location_WarehouseId",
                table: "Wms_Location",
                column: "WarehouseId");

            migrationBuilder.DropIndex(
                name: "IX_Wms_Zone_Code",
                table: "Wms_Zone");

            migrationBuilder.DropIndex(
                name: "IX_Wms_Zone_WarehouseId_PackId",
                table: "Wms_Zone");

            migrationBuilder.DropIndex(
                name: "IX_Wms_Location_AisleId",
                table: "Wms_Location");

            migrationBuilder.DropIndex(
                name: "IX_Wms_Location_LayerId",
                table: "Wms_Location");

            migrationBuilder.DropIndex(
                name: "IX_Wms_Location_WarehouseId_PackId",
                table: "Wms_Location");

            migrationBuilder.DropColumn(
                name: "PackId",
                table: "Wms_Zone");

            migrationBuilder.DropColumn(
                name: "EnabledPackIds",
                table: "Wms_Warehouse");

            migrationBuilder.DropColumn(
                name: "AisleId",
                table: "Wms_Location");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "Wms_Location");

            migrationBuilder.DropColumn(
                name: "IsBooked",
                table: "Wms_Location");

            migrationBuilder.DropColumn(
                name: "LayerId",
                table: "Wms_Location");

            migrationBuilder.DropColumn(
                name: "PackId",
                table: "Wms_Location");

            migrationBuilder.AlterColumn<string>(
                name: "Aisle",
                table: "Wms_Location",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
