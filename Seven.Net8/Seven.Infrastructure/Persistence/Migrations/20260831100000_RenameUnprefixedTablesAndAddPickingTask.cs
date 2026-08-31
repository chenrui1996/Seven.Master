using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameUnprefixedTablesAndAddPickingTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(name: "Device", newName: "Biz_Device");
            migrationBuilder.RenameTable(name: "SubDevice", newName: "Biz_SubDevice");
            migrationBuilder.RenameTable(name: "CommConnection", newName: "Dc_CommConnection");
            migrationBuilder.RenameTable(name: "CommPoint", newName: "Dc_CommPoint");
            migrationBuilder.RenameTable(name: "CommRule", newName: "Dc_CommRule");
            migrationBuilder.RenameTable(name: "CommEventLog", newName: "Dc_CommEventLog");
            migrationBuilder.RenameTable(name: "FormCollectionObject", newName: "Form_CollectionObject");
            migrationBuilder.RenameTable(name: "FormDesignOptions", newName: "Form_DesignOptions");
            migrationBuilder.RenameTable(name: "OutboxMessages", newName: "Mq_OutboxMessages");

            migrationBuilder.CreateTable(
                name: "Wms_PickingTask",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TaskNo = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OutboundOrderId = table.Column<int>(type: "int", nullable: false),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    MaterialCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BookQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PickQty = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    FromLocation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToLocation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContainerCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WcsPri = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TransportOrderId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_Wms_PickingTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wms_PickingTask_Wms_OutboundOrder_OutboundOrderId",
                        column: x => x.OutboundOrderId,
                        principalTable: "Wms_OutboundOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wms_PickingTask_Wms_OutboundOrderLine_LineId",
                        column: x => x.LineId,
                        principalTable: "Wms_OutboundOrderLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_PickingTask_LineId",
                table: "Wms_PickingTask",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_PickingTask_OutboundOrderId_LineId",
                table: "Wms_PickingTask",
                columns: new[] { "OutboundOrderId", "LineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Wms_PickingTask_Status",
                table: "Wms_PickingTask",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_PickingTask_TaskNo",
                table: "Wms_PickingTask",
                column: "TaskNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Wms_PickingTask");

            migrationBuilder.RenameTable(name: "Biz_Device", newName: "Device");
            migrationBuilder.RenameTable(name: "Biz_SubDevice", newName: "SubDevice");
            migrationBuilder.RenameTable(name: "Dc_CommConnection", newName: "CommConnection");
            migrationBuilder.RenameTable(name: "Dc_CommPoint", newName: "CommPoint");
            migrationBuilder.RenameTable(name: "Dc_CommRule", newName: "CommRule");
            migrationBuilder.RenameTable(name: "Dc_CommEventLog", newName: "CommEventLog");
            migrationBuilder.RenameTable(name: "Form_CollectionObject", newName: "FormCollectionObject");
            migrationBuilder.RenameTable(name: "Form_DesignOptions", newName: "FormDesignOptions");
            migrationBuilder.RenameTable(name: "Mq_OutboxMessages", newName: "OutboxMessages");
        }
    }
}
