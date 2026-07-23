using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSemanticObjectType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa dữ liệu cũ là 'obstacle' (do logic obstacle đã bị xóa bỏ)
            migrationBuilder.Sql("DELETE FROM SemanticObjects WHERE ObjectType = 'obstacle';");

            // Chuẩn hóa case-sensitive cho các giá trị Enum
            migrationBuilder.Sql("UPDATE SemanticObjects SET ObjectType = 'Shelf' WHERE LOWER(ObjectType) = 'shelf';");
            migrationBuilder.Sql("UPDATE SemanticObjects SET ObjectType = 'Zone' WHERE LOWER(ObjectType) = 'zone';");
            migrationBuilder.Sql("UPDATE SemanticObjects SET ObjectType = 'ProductShelf' WHERE LOWER(ObjectType) = 'productshelf';");
            
            // Xóa các loại không hợp lệ còn sót lại nếu có
            migrationBuilder.Sql("DELETE FROM SemanticObjects WHERE ObjectType NOT IN ('Shelf', 'Zone', 'ProductShelf');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
