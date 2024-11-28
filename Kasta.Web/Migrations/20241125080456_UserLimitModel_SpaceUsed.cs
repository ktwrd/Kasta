using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Web.Migrations
{
    /// <inheritdoc />
    public partial class UserLimitModel_SpaceUsed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SpaceUsed",
                table: "UserLimits",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpaceUsed",
                table: "UserLimits");
        }
    }
}
