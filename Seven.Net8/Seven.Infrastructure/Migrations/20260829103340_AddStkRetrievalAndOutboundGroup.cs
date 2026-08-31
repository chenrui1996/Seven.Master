using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStkRetrievalAndOutboundGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WcsPri",
                table: "Wms_OutboundOrderLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WcsGroupNo",
                table: "Wms_OutboundOrder",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<Guid>(
                name: "PutAwayTaskId",
                table: "Stk_DeviceTask",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "RetrievalTaskId",
                table: "Stk_DeviceTask",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "WcsGroupNo",
                table: "Bus_TransportOrder",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "WcsPri",
                table: "Bus_TransportOrder",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Stk_RetrievalTask",
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
                    WcsGroupNo = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WcsPri = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Stk_RetrievalTask", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Stk_DeviceTask_RetrievalTaskId",
                table: "Stk_DeviceTask",
                column: "RetrievalTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Stk_RetrievalTask_ContainerCode",
                table: "Stk_RetrievalTask",
                column: "ContainerCode");

            migrationBuilder.CreateIndex(
                name: "IX_Stk_RetrievalTask_LegId",
                table: "Stk_RetrievalTask",
                column: "LegId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stk_RetrievalTask_Status",
                table: "Stk_RetrievalTask",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Stk_RetrievalTask_WcsGroupNo_WcsPri",
                table: "Stk_RetrievalTask",
                columns: new[] { "WcsGroupNo", "WcsPri" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stk_RetrievalTask");

            migrationBuilder.DropIndex(
                name: "IX_Stk_DeviceTask_RetrievalTaskId",
                table: "Stk_DeviceTask");

            migrationBuilder.DropColumn(
                name: "WcsPri",
                table: "Wms_OutboundOrderLine");

            migrationBuilder.DropColumn(
                name: "WcsGroupNo",
                table: "Wms_OutboundOrder");

            migrationBuilder.DropColumn(
                name: "RetrievalTaskId",
                table: "Stk_DeviceTask");

            migrationBuilder.DropColumn(
                name: "WcsGroupNo",
                table: "Bus_TransportOrder");

            migrationBuilder.DropColumn(
                name: "WcsPri",
                table: "Bus_TransportOrder");

            migrationBuilder.AlterColumn<Guid>(
                name: "PutAwayTaskId",
                table: "Stk_DeviceTask",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");
        }
    }
}
