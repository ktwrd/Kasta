using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kate.FileShare.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChunkUploadSession_File_FileId",
                table: "ChunkUploadSession");

            migrationBuilder.AddColumn<string>(
                name: "S3FileInformationId",
                table: "File",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_File_S3FileInformationId",
                table: "File",
                column: "S3FileInformationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChunkUploadSession_File_FileId",
                table: "ChunkUploadSession",
                column: "FileId",
                principalTable: "File",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_File_S3FileInformation_S3FileInformationId",
                table: "File",
                column: "S3FileInformationId",
                principalTable: "S3FileInformation",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChunkUploadSession_File_FileId",
                table: "ChunkUploadSession");

            migrationBuilder.DropForeignKey(
                name: "FK_File_S3FileInformation_S3FileInformationId",
                table: "File");

            migrationBuilder.DropIndex(
                name: "IX_File_S3FileInformationId",
                table: "File");

            migrationBuilder.DropColumn(
                name: "S3FileInformationId",
                table: "File");

            migrationBuilder.AddForeignKey(
                name: "FK_ChunkUploadSession_File_FileId",
                table: "ChunkUploadSession",
                column: "FileId",
                principalTable: "File",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
