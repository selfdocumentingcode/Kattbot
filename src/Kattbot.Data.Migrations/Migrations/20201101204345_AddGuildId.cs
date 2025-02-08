using Microsoft.EntityFrameworkCore.Migrations;

namespace Kattbot.Data.Migrations
{
    public partial class AddGuildId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "Emotes",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Emotes");
        }
    }
}
