using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFwMapVersionLayerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LayerCode",
                table: "Fw_MapVersion",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Fw_MapVersion_LayerCode",
                table: "Fw_MapVersion",
                column: "LayerCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fw_MapVersion_LayerCode",
                table: "Fw_MapVersion");

            migrationBuilder.DropColumn(
                name: "LayerCode",
                table: "Fw_MapVersion");
        }
    }
}
