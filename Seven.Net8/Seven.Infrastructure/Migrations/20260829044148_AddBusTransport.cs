using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusTransport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bus_TransportOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ContainerCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromLocationCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToLocationCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailReason = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RefId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
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
                    table.PrimaryKey("PK_Bus_TransportOrder", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Wms_HandoverLink",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FromPackId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToPackId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocationCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
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
                    table.PrimaryKey("PK_Wms_HandoverLink", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bus_TransportLeg",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PackId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Seq = table.Column<int>(type: "int", nullable: false),
                    FromCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContainerCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandoverIn = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HandoverOut = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
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
                    table.PrimaryKey("PK_Bus_TransportLeg", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bus_TransportLeg_Bus_TransportOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Bus_TransportOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Bus_TransportLeg_OrderId_Seq",
                table: "Bus_TransportLeg",
                columns: new[] { "OrderId", "Seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bus_TransportLeg_Status",
                table: "Bus_TransportLeg",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bus_TransportOrder_Status",
                table: "Bus_TransportOrder",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Wms_HandoverLink_FromPackId_ToPackId_LocationCode",
                table: "Wms_HandoverLink",
                columns: new[] { "FromPackId", "ToPackId", "LocationCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bus_TransportLeg");

            migrationBuilder.DropTable(
                name: "Wms_HandoverLink");

            migrationBuilder.DropTable(
                name: "Bus_TransportOrder");
        }
    }
}
