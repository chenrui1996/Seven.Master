using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Seven.Infrastructure.Persistence.Migrations
{
    /// <summary>下线业务 Demo：Biz_Device / Biz_SubDevice</summary>
    public partial class DropBizDeviceDemo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Biz_SubDevice");
            migrationBuilder.DropTable(name: "Biz_Device");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Demo 表不恢复；如需回滚请从历史迁移重建。
        }
    }
}
