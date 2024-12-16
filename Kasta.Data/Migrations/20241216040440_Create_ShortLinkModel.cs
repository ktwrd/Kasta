using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.Migrations
{
    /// <inheritdoc />
    public partial class Create_ShortLinkModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShortLinks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    Destination = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ShortLink = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsVanity = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShortLinks_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_CreatedByUserId",
                table: "ShortLinks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_ShortLink",
                table: "ShortLinks",
                column: "ShortLink",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShortLinks");
        }
    }
}
