using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DespesasAvulsas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TipoDespesa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoDespesa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipoDespesa_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Despesa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    VeiculoId = table.Column<int>(type: "integer", nullable: false),
                    TipoDespesaId = table.Column<int>(type: "integer", nullable: false),
                    MotoristaId = table.Column<int>(type: "integer", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DataDespesa = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despesa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Despesa_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Despesa_TipoDespesa_TipoDespesaId",
                        column: x => x.TipoDespesaId,
                        principalTable: "TipoDespesa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Despesa_Usuario_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Despesa_Veiculo_VeiculoId",
                        column: x => x.VeiculoId,
                        principalTable: "Veiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Despesa_EmpresaId_MotoristaId_DataDespesa",
                table: "Despesa",
                columns: new[] { "EmpresaId", "MotoristaId", "DataDespesa" });

            migrationBuilder.CreateIndex(
                name: "IX_Despesa_EmpresaId_VeiculoId_DataDespesa",
                table: "Despesa",
                columns: new[] { "EmpresaId", "VeiculoId", "DataDespesa" });

            migrationBuilder.CreateIndex(
                name: "IX_Despesa_MotoristaId",
                table: "Despesa",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_Despesa_TipoDespesaId",
                table: "Despesa",
                column: "TipoDespesaId");

            migrationBuilder.CreateIndex(
                name: "IX_Despesa_VeiculoId",
                table: "Despesa",
                column: "VeiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_TipoDespesa_EmpresaId_Nome",
                table: "TipoDespesa",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            // Semeia o catálogo das empresas que já existem. O BackofficeService só cobre
            // empresa nova; sem isto, todo cliente atual abriria a tela de despesas com o
            // seletor de tipo vazio, sem conseguir lançar nada.
            //
            // A lista está duplicada de TiposDespesaPadrao.Itens de propósito: migration é
            // artefato histórico e não acompanha mudanças naquela constante.
            migrationBuilder.Sql("""
                INSERT INTO "TipoDespesa" ("EmpresaId", "Nome", "Ativo", "DataInclusao")
                SELECT e."Id", t.nome, true, CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'
                FROM "Empresa" e
                CROSS JOIN (VALUES ('Pedágio'), ('Multa de trânsito'), ('IPVA'),
                                   ('Licenciamento'), ('Seguro'), ('Lavagem'),
                                   ('Estacionamento')) AS t(nome);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Despesa");

            migrationBuilder.DropTable(
                name: "TipoDespesa");
        }
    }
}
