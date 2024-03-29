using Microsoft.EntityFrameworkCore.Migrations;

namespace Kattbot.Data.Migrations
{
    public partial class RenameEmoteIdToEmoteCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmoteId",
                newName: "EmoteCode",
                table: "Emotes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmoteCode",
                newName: "EmoteId",
                table: "Emotes");
        }
    }
}
