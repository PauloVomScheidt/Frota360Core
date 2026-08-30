using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbastecimento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Abastecimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    VeiculoId = table.Column<int>(type: "int", nullable: false),
                    RotaId = table.Column<int>(type: "int", nullable: true),
                    MotoristaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    DataAbastecimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abastecimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Abastecimento_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abastecimento_Rota_RotaId",
                        column: x => x.RotaId,
                        principalTable: "Rota",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Abastecimento_Usuario_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abastecimento_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Abastecimento_Veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_EmpresaId_MotoristaId_DataAbastecimento",
                table: "Abastecimento",
                columns: new[] { "EmpresaId", "MotoristaId", "DataAbastecimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_EmpresaId_VeiculoId_DataAbastecimento",
                table: "Abastecimento",
                columns: new[] { "EmpresaId", "VeiculoId", "DataAbastecimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_MotoristaId",
                table: "Abastecimento",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_RotaId",
                table: "Abastecimento",
                column: "RotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_UsuarioId",
                table: "Abastecimento",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_VeiculoId",
                table: "Abastecimento",
                column: "VeiculoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Abastecimento");
        }
    }
}
