using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasta.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserApiKeyModel_ExtendAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByIpAddress",
                table: "UserApiKeys",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserAgent",
                table: "UserApiKeys",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "UserApiKeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByIpAddress",
                table: "UserApiKeys",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserAgent",
                table: "UserApiKeys",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "UserApiKeys",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserApiKeys",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUsed",
                table: "UserApiKeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserApiKeys_DeletedByUserId",
                table: "UserApiKeys",
                column: "DeletedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserApiKeys_AspNetUsers_DeletedByUserId",
                table: "UserApiKeys",
                column: "DeletedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserApiKeys_AspNetUsers_DeletedByUserId",
                table: "UserApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_UserApiKeys_DeletedByUserId",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "CreatedByIpAddress",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "CreatedByUserAgent",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "DeletedByIpAddress",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "DeletedByUserAgent",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserApiKeys");

            migrationBuilder.DropColumn(
                name: "LastUsed",
                table: "UserApiKeys");
        }
    }
}
