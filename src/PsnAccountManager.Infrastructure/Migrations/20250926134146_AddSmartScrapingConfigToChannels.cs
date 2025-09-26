using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsnAccountManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartScrapingConfigToChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DelayAfterScrapeMs",
                table: "Channels",
                type: "int",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddColumn<string>(
                name: "FetchMode",
                table: "Channels",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "SinceLastMessage")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "FetchValue",
                table: "Channels",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelayAfterScrapeMs",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "FetchMode",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "FetchValue",
                table: "Channels");
        }
    }
}
