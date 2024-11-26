using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kate.FileShare.Migrations
{
    /// <inheritdoc />
    public partial class FilePreview_Filename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Filename",
                table: "FilePreview",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Filename",
                table: "FilePreview");
        }
    }
}
