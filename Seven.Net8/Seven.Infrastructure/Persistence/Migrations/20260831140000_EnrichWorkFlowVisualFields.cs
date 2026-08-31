using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Persistence.Migrations;

/// <summary>工作流可视化与审批进度字段对齐 Legrand</summary>
public partial class EnrichWorkFlowVisualFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "WorkTableName", table: "Sys_WorkFlow", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<int>(name: "Weight", table: "Sys_WorkFlow", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "NodeConfig", table: "Sys_WorkFlow", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "LineConfig", table: "Sys_WorkFlow", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "Remark", table: "Sys_WorkFlow", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<int>(name: "AuditingEdit", table: "Sys_WorkFlow", type: "int", nullable: true);

        migrationBuilder.AddColumn<string>(name: "StepId", table: "Sys_WorkFlowStep", type: "varchar(100)", maxLength: 100, nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "StepAttrType", table: "Sys_WorkFlowStep", type: "varchar(50)", maxLength: 50, nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "NextStepIds", table: "Sys_WorkFlowStep", type: "varchar(500)", maxLength: 500, nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "ParentId", table: "Sys_WorkFlowStep", type: "varchar(200)", maxLength: 200, nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<int>(name: "Weight", table: "Sys_WorkFlowStep", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Filters", table: "Sys_WorkFlowStep", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<int>(name: "AuditRefuse", table: "Sys_WorkFlowStep", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "AuditBack", table: "Sys_WorkFlowStep", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "AuditMethod", table: "Sys_WorkFlowStep", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>(name: "SendMail", table: "Sys_WorkFlowStep", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Remark", table: "Sys_WorkFlowStep", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(name: "StepName", table: "Sys_WorkFlowTableStep", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "Auditor", table: "Sys_WorkFlowTableStep", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>(name: "Remark", table: "Sys_WorkFlowTableStep", type: "longtext", nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "WorkTableName", table: "Sys_WorkFlow");
        migrationBuilder.DropColumn(name: "Weight", table: "Sys_WorkFlow");
        migrationBuilder.DropColumn(name: "NodeConfig", table: "Sys_WorkFlow");
        migrationBuilder.DropColumn(name: "LineConfig", table: "Sys_WorkFlow");
        migrationBuilder.DropColumn(name: "Remark", table: "Sys_WorkFlow");
        migrationBuilder.DropColumn(name: "AuditingEdit", table: "Sys_WorkFlow");

        migrationBuilder.DropColumn(name: "StepId", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "StepAttrType", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "NextStepIds", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "ParentId", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "Weight", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "Filters", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "AuditRefuse", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "AuditBack", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "AuditMethod", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "SendMail", table: "Sys_WorkFlowStep");
        migrationBuilder.DropColumn(name: "Remark", table: "Sys_WorkFlowStep");

        migrationBuilder.DropColumn(name: "StepName", table: "Sys_WorkFlowTableStep");
        migrationBuilder.DropColumn(name: "Auditor", table: "Sys_WorkFlowTableStep");
        migrationBuilder.DropColumn(name: "Remark", table: "Sys_WorkFlowTableStep");
    }
}
