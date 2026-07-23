using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketBot.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase B — Line-scanning navigation refactor (Step 2/4).
    /// Adds CurrentNodeCode column to ROBOT_LOG for firmware line-scan telemetry.
    /// Nullable: log cũ giữ NULL (legacy AMR chỉ gửi X/Y/CurrentNodeId).
    ///
    /// Run via:
    ///   dotnet ef database update
    /// Hoặc apply trực tiếp script db/migrations/migration_robot_log_current_node_code.sql
    /// </summary>
    public partial class AddCurrentNodeCodeToRobotLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentNodeCode",
                table: "ROBOT_LOG",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_LOG_CurrentNodeCode",
                table: "ROBOT_LOG",
                column: "CurrentNodeCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ROBOT_LOG_CurrentNodeCode",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "CurrentNodeCode",
                table: "ROBOT_LOG");
        }
    }
}
