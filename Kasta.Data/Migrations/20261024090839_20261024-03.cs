using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.Migrations
{
    /// <inheritdoc />
    public partial class _2026102403 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_File_S3FileInformation_S3FileInformationId",
                table: "File");

            migrationBuilder.DropForeignKey(
                name: "FK_FileImageInfo_File_Id",
                table: "FileImageInfo");

            migrationBuilder.DropForeignKey(
                name: "FK_S3FileInformation_File_Id",
                table: "S3FileInformation");

            migrationBuilder.DropIndex(
                name: "IX_File_S3FileInformationId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "S3FileInformationId",
                table: "File");

            migrationBuilder.AddForeignKey(
                name: "FK_FileImageInfo_File_Id",
                table: "FileImageInfo",
                column: "Id",
                principalTable: "File",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_S3FileInformation_File_Id",
                table: "S3FileInformation",
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

            migrationBuilder.DropForeignKey(
                name: "FK_S3FileInformation_File_Id",
                table: "S3FileInformation");

            migrationBuilder.AddColumn<string>(
                name: "S3FileInformationId",
                table: "File",
                type: "character varying(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_File_S3FileInformationId",
                table: "File",
                column: "S3FileInformationId");

            migrationBuilder.AddForeignKey(
                name: "FK_File_S3FileInformation_S3FileInformationId",
                table: "File",
                column: "S3FileInformationId",
                principalTable: "S3FileInformation",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FileImageInfo_File_Id",
                table: "FileImageInfo",
                column: "Id",
                principalTable: "File",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_S3FileInformation_File_Id",
                table: "S3FileInformation",
                column: "Id",
                principalTable: "File",
                principalColumn: "Id");
        }
    }
}
