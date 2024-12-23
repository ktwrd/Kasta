using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateTables_TrustedProxy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Config_TrustedProxy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Address = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Enable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Config_TrustedProxy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Config_TrustedProxyHeader",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    HeaderName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Enable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Config_TrustedProxyHeader", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Config_TrustedProxyHeaderMapping",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    TrustedProxyHeaderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    TrustedProxyId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Config_TrustedProxyHeaderMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Config_TrustedProxyHeaderMapping_Config_TrustedProxyHeader_~",
                        column: x => x.TrustedProxyHeaderId,
                        principalTable: "Config_TrustedProxyHeader",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Config_TrustedProxyHeaderMapping_Config_TrustedProxy_Truste~",
                        column: x => x.TrustedProxyId,
                        principalTable: "Config_TrustedProxy",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Config_TrustedProxy_Address",
                table: "Config_TrustedProxy",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Config_TrustedProxyHeader_HeaderName",
                table: "Config_TrustedProxyHeader",
                column: "HeaderName");

            migrationBuilder.CreateIndex(
                name: "IX_Config_TrustedProxyHeaderMapping_TrustedProxyHeaderId",
                table: "Config_TrustedProxyHeaderMapping",
                column: "TrustedProxyHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Config_TrustedProxyHeaderMapping_TrustedProxyId",
                table: "Config_TrustedProxyHeaderMapping",
                column: "TrustedProxyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Config_TrustedProxyHeaderMapping");

            migrationBuilder.DropTable(
                name: "Config_TrustedProxyHeader");

            migrationBuilder.DropTable(
                name: "Config_TrustedProxy");
        }
    }
}
