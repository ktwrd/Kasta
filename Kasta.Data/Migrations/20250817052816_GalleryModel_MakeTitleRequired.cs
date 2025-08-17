using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.Migrations
{
    /// <inheritdoc />
    public partial class GalleryModel_MakeTitleRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gallery",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Public = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gallery", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gallery_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GalleryFileAssociation",
                columns: table => new
                {
                    GalleryId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FileId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryFileAssociation", x => new { x.GalleryId, x.FileId });
                    table.ForeignKey(
                        name: "FK_GalleryFileAssociation_File_FileId",
                        column: x => x.FileId,
                        principalTable: "File",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalleryFileAssociation_Gallery_GalleryId",
                        column: x => x.GalleryId,
                        principalTable: "Gallery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalleryTextHistory",
                columns: table => new
                {
                    GalleryId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryTextHistory", x => new { x.GalleryId, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_GalleryTextHistory_Gallery_GalleryId",
                        column: x => x.GalleryId,
                        principalTable: "Gallery",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gallery_CreatedByUserId",
                table: "Gallery",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Gallery_Public",
                table: "Gallery",
                column: "Public");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryFileAssociation_FileId",
                table: "GalleryFileAssociation",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalleryTextHistory_GalleryId",
                table: "GalleryTextHistory",
                column: "GalleryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalleryTextHistory_Timestamp",
                table: "GalleryTextHistory",
                column: "Timestamp",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GalleryFileAssociation");

            migrationBuilder.DropTable(
                name: "GalleryTextHistory");

            migrationBuilder.DropTable(
                name: "Gallery");
        }
    }
}
