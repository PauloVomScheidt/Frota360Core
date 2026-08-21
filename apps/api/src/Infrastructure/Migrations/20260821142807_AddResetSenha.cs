using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResetSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ResetSenhaExpiraEm",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetSenhaTokenHash",
                table: "Usuario",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_ResetSenhaTokenHash",
                table: "Usuario",
                column: "ResetSenhaTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuario_ResetSenhaTokenHash",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ResetSenhaExpiraEm",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "ResetSenhaTokenHash",
                table: "Usuario");
        }
    }
}
