using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketBot.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase B — Line-scanning navigation refactor (Step 1/4).
    /// Adds NodeCode column to NAVIGATION_NODE. Backfill for existing rows
    /// uses 'NODE_{NodeId}'. New writes from MapSyncService populate NodeCode
    /// explicitly.
    ///
    /// Run via:
    ///   dotnet ef database update
    /// Hoặc apply trực tiếp script db/migrations/migration_navigation_node_code.sql
    /// </summary>
    public partial class AddNodeCodeToNavigationNode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NodeCode",
                table: "NAVIGATION_NODE",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Backfill NodeCode cho row đã tồn tại: NODE_{NodeId} (unique per map).
            migrationBuilder.Sql(
                "UPDATE NAVIGATION_NODE SET NodeCode = 'NODE_' + CAST(NodeId AS NVARCHAR(10)) " +
                "WHERE NodeCode IS NULL OR NodeCode = ''");

            migrationBuilder.CreateIndex(
                name: "IX_NAVIGATION_NODE_MapID_NodeCode",
                table: "NAVIGATION_NODE",
                columns: new[] { "MapID", "NodeCode" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NAVIGATION_NODE_MapID_NodeCode",
                table: "NAVIGATION_NODE");

            migrationBuilder.DropColumn(
                name: "NodeCode",
                table: "NAVIGATION_NODE");
        }
    }
}
