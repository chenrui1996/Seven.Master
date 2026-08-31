using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStkAssignmentPolicyEmptyWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AllocationWeight",
                table: "Stk_AssignmentPolicy",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MinEmptySlots",
                table: "Stk_AssignmentPolicy",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocationWeight",
                table: "Stk_AssignmentPolicy");

            migrationBuilder.DropColumn(
                name: "MinEmptySlots",
                table: "Stk_AssignmentPolicy");
        }
    }
}
