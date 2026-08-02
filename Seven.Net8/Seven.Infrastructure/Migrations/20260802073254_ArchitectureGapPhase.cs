using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ArchitectureGapPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_WorkFlowTableStep",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_WorkFlowTableAuditLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_WorkFlowTable",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_WorkFlowStep",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_WorkFlow",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_UserDepartment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_User",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_TableInfo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_TableDetail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_TableColumn",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_RoleAuth",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DataScope",
                table: "Sys_Role",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_Role",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_QuartzOptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_QuartzLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_Menu",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_Log",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_DictionaryList",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_Dictionary",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_Department",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_AlarmCode",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Sys_Alarm",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SubDevice",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "FormDesignOptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "FormCollectionObject",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Device",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "App_News",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MessageType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Error = table.Column<string>(type: "longtext", nullable: true)
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
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_WorkFlowTableStep");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_WorkFlowTableAuditLog");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_WorkFlowTable");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_WorkFlowStep");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_WorkFlow");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_UserDepartment");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_User");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_TableDetail");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_RoleAuth");

            migrationBuilder.DropColumn(
                name: "DataScope",
                table: "Sys_Role");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_Role");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_QuartzOptions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_QuartzLog");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_Menu");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_Log");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_DictionaryList");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_Dictionary");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_Department");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_AlarmCode");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Sys_Alarm");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SubDevice");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FormDesignOptions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FormCollectionObject");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Device");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "App_News");
        }
    }
}
