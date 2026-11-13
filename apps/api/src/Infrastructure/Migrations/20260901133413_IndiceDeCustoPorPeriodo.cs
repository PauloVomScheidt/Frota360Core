using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndiceDeCustoPorPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Manutencao_EmpresaId_DataRealizacao",
                table: "Manutencao",
                columns: new[] { "EmpresaId", "DataRealizacao" },
                filter: "\"DataRealizacao\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Manutencao_EmpresaId_DataRealizacao",
                table: "Manutencao");
        }
    }
}
