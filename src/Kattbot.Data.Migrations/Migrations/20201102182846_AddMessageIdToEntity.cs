using Microsoft.EntityFrameworkCore.Migrations;

namespace Kattbot.Data.Migrations
{
    public partial class AddMessageIdToEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MessageId",
                table: "Emotes",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Emotes");
        }
    }
}
