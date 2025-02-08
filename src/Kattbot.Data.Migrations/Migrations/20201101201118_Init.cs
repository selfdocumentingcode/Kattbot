using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kattbot.Data.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Emotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    EmoteId = table.Column<string>(nullable: false),
                    DateTime = table.Column<DateTimeOffset>(nullable: false),
                    Source = table.Column<int>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emotes", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Emotes");
        }
    }
}
