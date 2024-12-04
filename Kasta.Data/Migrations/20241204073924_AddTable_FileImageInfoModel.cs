using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTable_FileImageInfoModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileImageInfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<long>(type: "bigint", nullable: false),
                    Height = table.Column<long>(type: "bigint", nullable: false),
                    ColorSpace = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompressionMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MagickFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Interlace = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompressionLevel = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileImageInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileImageInfo_File_Id",
                        column: x => x.Id,
                        principalTable: "File",
                        principalColumn: "Id");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileImageInfo");
        }
    }
}
