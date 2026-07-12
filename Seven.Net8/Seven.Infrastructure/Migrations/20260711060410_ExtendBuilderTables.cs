using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendBuilderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CnName",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DBServer",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DataTableType",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DetailName",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EditorType",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Enable",
                table: "Sys_TableInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpressField",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FolderName",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "OrderNo",
                table: "Sys_TableInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Sys_TableInfo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SortName",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TableTrueName",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UploadField",
                table: "Sys_TableInfo",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ColSize",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ColumnWidth",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Columnformat",
                table: "Sys_TableColumn",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DropNo",
                table: "Sys_TableColumn",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EditColNo",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EditRowNo",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditType",
                table: "Sys_TableColumn",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Enable",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsColumnData",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsDisplay",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsImage",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsNull",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsReadDataset",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Maxlength",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderNo",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "Sys_TableColumn",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SearchColNo",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SearchRowNo",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchType",
                table: "Sys_TableColumn",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Sortable",
                table: "Sys_TableColumn",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableName",
                table: "Sys_TableColumn",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CnName",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "DBServer",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "DataTableType",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "DetailName",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "EditorType",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "Enable",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "ExpressField",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "FolderName",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "OrderNo",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "SortName",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "TableTrueName",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "UploadField",
                table: "Sys_TableInfo");

            migrationBuilder.DropColumn(
                name: "ColSize",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "ColumnWidth",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "Columnformat",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "DropNo",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "EditColNo",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "EditRowNo",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "EditType",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "Enable",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "IsColumnData",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "IsDisplay",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "IsImage",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "IsNull",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "IsReadDataset",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "Maxlength",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "OrderNo",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "Script",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "SearchColNo",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "SearchRowNo",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "SearchType",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "Sortable",
                table: "Sys_TableColumn");

            migrationBuilder.DropColumn(
                name: "TableName",
                table: "Sys_TableColumn");
        }
    }
}
