using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketBot.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 1: Phân loại & liên kết Zone cho RobotRoute.
    /// Viết tay (không dùng dotnet ef scaffold) để tránh sinh thêm các thao tác thừa
    /// do model snapshot lệch DB thật (ROBOT_ZONE, ProductID→ProductTypeID...).
    /// DB Azure hiện tại (đối chiếu trực tiếp INFORMATION_SCHEMA):
    ///   - ROBOT_ROUTE chỉ có 5 cột (RobotRouteID, RobotID, MapID, RouteName, CreatedAt)
    ///   - ZONE có ZoneID PRIMARY KEY
    ///   - Không có ROBOT_ZONE
    ///   - Tất cả các bảng đều rỗng (cnt = 0)
    /// → Thao tác ADD COLUMN + FK + INDEX an toàn, không động vào dữ liệu khác.
    /// </summary>
    public partial class AddZoneAndTypeToRobotRoute : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm 3 cột mới vào ROBOT_ROUTE
            migrationBuilder.AddColumn<int>(
                name: "ZoneID",
                table: "ROBOT_ROUTE",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteType",
                table: "ROBOT_ROUTE",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "patrol");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ROBOT_ROUTE",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // 2. Index phục vụ filter (lọc theo Zone, lọc theo RouteType)
            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_ROUTE_ZoneID",
                table: "ROBOT_ROUTE",
                column: "ZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_ROUTE_RouteType",
                table: "ROBOT_ROUTE",
                column: "RouteType");
            // NOTE: FK đến ZONE(ZoneID) không tạo được vì SQL Server Azure phát hiện
            // "multiple cascade paths" (chu trình: ROBOT_ROUTE→MAP CASCADE, ROBOT_ROUTE→ROUTE_ASSIGNMENT CASCADE,
            // thêm ROBOT_ROUTE→ZONE CASCADE sẽ tạo chu trình cascade).
            // Giải pháp: chỉ tạo INDEX + nullable column. Tham chiếu ZoneId hợp lệ vẫn được
            // đảm bảo ở tầng API (kiểm tra ZoneId tồn tại trước khi gán).
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ROBOT_ROUTE_RouteType",
                table: "ROBOT_ROUTE");

            migrationBuilder.DropIndex(
                name: "IX_ROBOT_ROUTE_ZoneID",
                table: "ROBOT_ROUTE");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ROBOT_ROUTE");

            migrationBuilder.DropColumn(
                name: "RouteType",
                table: "ROBOT_ROUTE");

            migrationBuilder.DropColumn(
                name: "ZoneID",
                table: "ROBOT_ROUTE");
        }
    }
}