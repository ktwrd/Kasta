using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserModel_ClientCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserLimits_AspNetUsers_UserId",
                table: "UserLimits");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_AspNetUsers_Id",
                table: "UserSettings");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLimits_AspNetUsers_UserId",
                table: "UserLimits",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_AspNetUsers_Id",
                table: "UserSettings",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserLimits_AspNetUsers_UserId",
                table: "UserLimits");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_AspNetUsers_Id",
                table: "UserSettings");

            migrationBuilder.AddForeignKey(
                name: "FK_UserLimits_AspNetUsers_UserId",
                table: "UserLimits",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_AspNetUsers_Id",
                table: "UserSettings",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
