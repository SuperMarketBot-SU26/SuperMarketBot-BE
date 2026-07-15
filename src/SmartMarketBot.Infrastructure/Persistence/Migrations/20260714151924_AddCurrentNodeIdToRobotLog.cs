using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentNodeIdToRobotLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentNodeId",
                table: "ROBOT_LOG",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_LOG_CurrentNodeId",
                table: "ROBOT_LOG",
                column: "CurrentNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ROBOT_LOG_CurrentNodeId",
                table: "ROBOT_LOG");

            migrationBuilder.DropColumn(
                name: "CurrentNodeId",
                table: "ROBOT_LOG");
        }
    }
}
