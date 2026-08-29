using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFwParkingLayerAisle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AisleCode",
                table: "Fw_ParkingLedger",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LayerCode",
                table: "Fw_ParkingLedger",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Fw_ParkingLedger_LayerCode_AisleCode_Status",
                table: "Fw_ParkingLedger",
                columns: new[] { "LayerCode", "AisleCode", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fw_ParkingLedger_LayerCode_AisleCode_Status",
                table: "Fw_ParkingLedger");

            migrationBuilder.DropColumn(
                name: "AisleCode",
                table: "Fw_ParkingLedger");

            migrationBuilder.DropColumn(
                name: "LayerCode",
                table: "Fw_ParkingLedger");
        }
    }
}
