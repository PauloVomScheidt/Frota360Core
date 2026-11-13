using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlacaUnicaPorEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Veiculo_EmpresaId",
                table: "Veiculo");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_EmpresaId_Placa",
                table: "Veiculo",
                columns: new[] { "EmpresaId", "Placa" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Veiculo_EmpresaId_Placa",
                table: "Veiculo");

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_EmpresaId",
                table: "Veiculo",
                column: "EmpresaId");
        }
    }
}
