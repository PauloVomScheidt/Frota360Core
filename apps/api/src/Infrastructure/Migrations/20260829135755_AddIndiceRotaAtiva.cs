using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndiceRotaAtiva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rota_EmpresaId",
                table: "Rota");

            migrationBuilder.CreateIndex(
                name: "IX_Rota_EmpresaId_Ativo_CodigoVeiculo",
                table: "Rota",
                columns: new[] { "EmpresaId", "Ativo", "CodigoVeiculo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rota_EmpresaId_Ativo_CodigoVeiculo",
                table: "Rota");

            migrationBuilder.CreateIndex(
                name: "IX_Rota_EmpresaId",
                table: "Rota",
                column: "EmpresaId");
        }
    }
}
