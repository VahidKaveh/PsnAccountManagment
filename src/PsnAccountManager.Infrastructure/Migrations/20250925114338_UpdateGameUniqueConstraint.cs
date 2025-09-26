using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsnAccountManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGameUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_sony_code",
                table: "Games");

            migrationBuilder.AlterColumn<string>(
                name: "sony_code",
                table: "Games",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Games_title",
                table: "Games",
                column: "title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_title",
                table: "Games");

            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "sony_code",
                keyValue: null,
                column: "sony_code",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "sony_code",
                table: "Games",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Games_sony_code",
                table: "Games",
                column: "sony_code",
                unique: true);
        }
    }
}
