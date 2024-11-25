using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kate.FileShare.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "File",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Filename = table.Column<string>(type: "text", nullable: false),
                    RelativeLocation = table.Column<string>(type: "text", nullable: false),
                    ShortUrl = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_File", x => x.Id);
                    table.ForeignKey(
                        name: "FK_File_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChunkUploadSession",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    FileId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunkUploadSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChunkUploadSession_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChunkUploadSession_File_FileId",
                        column: x => x.FileId,
                        principalTable: "File",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "S3FileInformation",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ChunkSize = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_S3FileInformation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_S3FileInformation_File_Id",
                        column: x => x.Id,
                        principalTable: "File",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "S3FileChunk",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FileId = table.Column<string>(type: "text", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Sha256Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_S3FileChunk", x => x.Id);
                    table.ForeignKey(
                        name: "FK_S3FileChunk_S3FileInformation_FileId",
                        column: x => x.FileId,
                        principalTable: "S3FileInformation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChunkUploadSession_FileId",
                table: "ChunkUploadSession",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_ChunkUploadSession_UserId",
                table: "ChunkUploadSession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_File_CreatedByUserId",
                table: "File",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_S3FileChunk_FileId",
                table: "S3FileChunk",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChunkUploadSession");

            migrationBuilder.DropTable(
                name: "S3FileChunk");

            migrationBuilder.DropTable(
                name: "S3FileInformation");

            migrationBuilder.DropTable(
                name: "File");
        }
    }
}
