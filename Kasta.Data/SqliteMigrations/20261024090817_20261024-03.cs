using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.SqliteMigrations
{
    /// <inheritdoc />
    public partial class _2026102403 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileImageInfo_File_Id",
                table: "FileImageInfo");

            migrationBuilder.AddForeignKey(
                name: "FK_FileImageInfo_File_Id",
                table: "FileImageInfo",
                column: "Id",
                principalTable: "File",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileImageInfo_File_Id",
                table: "FileImageInfo");

            migrationBuilder.AddForeignKey(
                name: "FK_FileImageInfo_File_Id",
                table: "FileImageInfo",
                column: "Id",
                principalTable: "File",
                principalColumn: "Id");
        }
    }
}
