using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoAppApi.Migrations
{
    /// <inheritdoc />
    public partial class DenormalizeLikesCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Photos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                            name: "ThumbnailUrl",
                            table: "Photos",
                            type: "longtext",
                            nullable: true,
                            oldClrType: typeof(string),
                            oldType: "longtext")
                            .Annotation("MySql:CharSet", "utf8mb4")
                            .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                            name: "FileName",
                            table: "Photos",
                            type: "longtext",
                            nullable: true,
                            oldClrType: typeof(string),
                            oldType: "longtext")
                            .Annotation("MySql:CharSet", "utf8mb4")
                            .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "Photos");
            migrationBuilder.UpdateData(
                table: "Photos",
                keyColumn: "Url",
                keyValue: null,
                column: "Url",
                value: "");

            migrationBuilder.AlterColumn<string>(
                            name: "Url",
                            table: "Photos",
                            type: "longtext",
                            nullable: false,
                            oldClrType: typeof(string),
                            oldType: "longtext",
                            oldNullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4")
                            .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Photos",
                keyColumn: "ThumbnailUrl",
                keyValue: null,
                column: "ThumbnailUrl",
                value: "");

            migrationBuilder.AlterColumn<string>(
                            name: "ThumbnailUrl",
                            table: "Photos",
                            type: "longtext",
                            nullable: false,
                            oldClrType: typeof(string),
                            oldType: "longtext",
                            oldNullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4")
                            .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Photos",
                keyColumn: "FileName",
                keyValue: null,
                column: "FileName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                            name: "FileName",
                            table: "Photos",
                            type: "longtext",
                            nullable: false,
                            oldClrType: typeof(string),
                            oldType: "longtext",
                            oldNullable: true)
                            .Annotation("MySql:CharSet", "utf8mb4")
                            .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
