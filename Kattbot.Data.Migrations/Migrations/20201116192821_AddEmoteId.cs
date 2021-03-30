using Microsoft.EntityFrameworkCore.Migrations;

namespace Kattbot.Data.Migrations
{
    public partial class AddEmoteId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EmoteId",
                table: "Emotes",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "EmoteName",
                table: "Emotes",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EmoteAnimated",
                table: "Emotes",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmoteId",
                table: "Emotes");

            migrationBuilder.DropColumn(
                name: "EmoteName",
                table: "Emotes");

            migrationBuilder.DropColumn(
                name: "EmoteAnimated",
                table: "Emotes");
        }
    }
}
