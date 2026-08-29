using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Seven.Infrastructure.Persistence;

#nullable disable

namespace Seven.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SevenDbContext))]
    [Migration("20260829053000_AddInboundFromAndOutboundToLocation")]
    public partial class AddInboundFromAndOutboundToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FromLocation",
                table: "Wms_InboundOrderLine",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ToLocation",
                table: "Wms_OutboundOrderLine",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FromLocation",
                table: "Wms_InboundOrderLine");

            migrationBuilder.DropColumn(
                name: "ToLocation",
                table: "Wms_OutboundOrderLine");
        }
    }
}
