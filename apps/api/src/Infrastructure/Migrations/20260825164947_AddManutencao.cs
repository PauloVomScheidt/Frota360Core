using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManutencao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TipoManutencao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IntervaloKm = table.Column<int>(type: "int", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoManutencao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipoManutencao_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Manutencao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    VeiculoId = table.Column<int>(type: "int", nullable: false),
                    TipoManutencaoId = table.Column<int>(type: "int", nullable: false),
                    QuilometragemPrevista = table.Column<int>(type: "int", nullable: false),
                    DataPrevista = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuilometragemRealizada = table.Column<int>(type: "int", nullable: true),
                    DataRealizacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Custo = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manutencao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manutencao_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Manutencao_TipoManutencao_TipoManutencaoId",
                        column: x => x.TipoManutencaoId,
                        principalTable: "TipoManutencao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Manutencao_Veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Manutencao_EmpresaId_VeiculoId_Status",
                table: "Manutencao",
                columns: new[] { "EmpresaId", "VeiculoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Manutencao_TipoManutencaoId",
                table: "Manutencao",
                column: "TipoManutencaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Manutencao_VeiculoId",
                table: "Manutencao",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_TipoManutencao_EmpresaId_Nome",
                table: "TipoManutencao",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Manutencao");

            migrationBuilder.DropTable(
                name: "TipoManutencao");
        }
    }
}
