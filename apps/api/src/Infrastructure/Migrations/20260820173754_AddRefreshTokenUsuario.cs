using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiraEm",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "Usuario",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_RefreshTokenHash",
                table: "Usuario",
                column: "RefreshTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuario_RefreshTokenHash",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiraEm",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "Usuario");
        }
    }
}
