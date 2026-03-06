using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoAppApi.Migrations
{
    /// <inheritdoc />
    public partial class FixTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhotoTag",
                columns: table => new
                {
                    PhotosId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoTag", x => new { x.PhotosId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_PhotoTag_Photos_PhotosId",
                        column: x => x.PhotosId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTag_TagsId",
                table: "PhotoTag",
                column: "TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_TagTranslations_Tags_TagId",
                table: "TagTranslations",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TagTranslations_Tags_TagId",
                table: "TagTranslations");

            migrationBuilder.DropTable(
                name: "PhotoTag");
        }
    }
}
