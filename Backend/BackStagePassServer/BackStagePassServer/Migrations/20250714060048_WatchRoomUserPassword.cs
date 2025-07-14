using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackStagePassServer.Migrations
{
    /// <inheritdoc />
    public partial class WatchRoomUserPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "WatchRoomUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "WatchRooms",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WatchRoomUsers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "WatchRooms");
        }
    }
}
