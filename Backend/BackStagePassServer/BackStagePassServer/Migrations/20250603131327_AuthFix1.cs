using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackStagePassServer.Migrations
{
    /// <inheritdoc />
    public partial class AuthFix1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "EmailConfirms");

            migrationBuilder.DropColumn(
                name: "TempPasswordHash",
                table: "EmailConfirms");

            migrationBuilder.DropColumn(
                name: "TempUsername",
                table: "EmailConfirms");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsEmailConfirmed",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IsConfirmed",
                table: "EmailConfirms",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TempPasswordHash",
                table: "EmailConfirms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TempUsername",
                table: "EmailConfirms",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
