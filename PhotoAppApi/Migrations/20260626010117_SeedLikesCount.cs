using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoAppApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedLikesCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Photos p
                SET LikesCount = (
                    SELECT COUNT(*)
                    FROM PhotoLikes l
                    WHERE l.PhotoId = p.Id
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
