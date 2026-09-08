using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AbastecimentoDetalhadoECatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As colunas novas do apontamento são obrigatórias e apontam para catálogos que
            // ainda não existem — não há valor plausível para retroagir num lançamento antigo.
            // A base de desenvolvimento só tinha dados de mock, e a decisão de descartá-los foi
            // tomada junto ao stakeholder na virada para o abastecimento detalhado.
            migrationBuilder.Sql("""DELETE FROM "Abastecimento";""");

            migrationBuilder.AddColumn<string>(
                name: "Frentista",
                table: "Abastecimento",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Litros",
                table: "Abastecimento",
                type: "numeric(9,3)",
                precision: 9,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NotaFiscal",
                table: "Abastecimento",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Odometro",
                table: "Abastecimento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PostoId",
                table: "Abastecimento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoCombustivelId",
                table: "Abastecimento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorLitro",
                table: "Abastecimento",
                type: "numeric(8,3)",
                precision: 8,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Posto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    Cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posto_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TipoCombustivel",
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
                    table.PrimaryKey("PK_TipoCombustivel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipoCombustivel_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_PostoId",
                table: "Abastecimento",
                column: "PostoId");

            migrationBuilder.CreateIndex(
                name: "IX_Abastecimento_TipoCombustivelId",
                table: "Abastecimento",
                column: "TipoCombustivelId");

            migrationBuilder.CreateIndex(
                name: "IX_Posto_EmpresaId_Nome",
                table: "Posto",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TipoCombustivel_EmpresaId_Nome",
                table: "TipoCombustivel",
                columns: new[] { "EmpresaId", "Nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Abastecimento_Posto_PostoId",
                table: "Abastecimento",
                column: "PostoId",
                principalTable: "Posto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Abastecimento_TipoCombustivel_TipoCombustivelId",
                table: "Abastecimento",
                column: "TipoCombustivelId",
                principalTable: "TipoCombustivel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Semeia o catálogo das empresas que já existem. O BackofficeService só cobre
            // empresa nova; sem isto, todo cliente atual abriria a tela de abastecimento com
            // o seletor de combustível vazio, sem conseguir lançar nada.
            //
            // A lista está duplicada de TiposCombustivelPadrao.Itens de propósito: migration é
            // artefato histórico e não acompanha mudanças naquela constante.
            //
            // Não há semeadura de posto: rede credenciada não tem padrão, cada empresa
            // cadastra a sua.
            migrationBuilder.Sql("""
                INSERT INTO "TipoCombustivel" ("EmpresaId", "Nome", "Ativo", "DataInclusao")
                SELECT e."Id", t.nome, true, CURRENT_TIMESTAMP AT TIME ZONE 'America/Sao_Paulo'
                FROM "Empresa" e
                CROSS JOIN (VALUES ('Diesel S10'), ('Diesel S500'), ('Gasolina comum'),
                                   ('Gasolina aditivada'), ('Etanol'),
                                   ('GNV')) AS t(nome);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Abastecimento_Posto_PostoId",
                table: "Abastecimento");

            migrationBuilder.DropForeignKey(
                name: "FK_Abastecimento_TipoCombustivel_TipoCombustivelId",
                table: "Abastecimento");

            migrationBuilder.DropTable(
                name: "Posto");

            migrationBuilder.DropTable(
                name: "TipoCombustivel");

            migrationBuilder.DropIndex(
                name: "IX_Abastecimento_PostoId",
                table: "Abastecimento");

            migrationBuilder.DropIndex(
                name: "IX_Abastecimento_TipoCombustivelId",
                table: "Abastecimento");

            migrationBuilder.DropColumn(
                name: "Frentista",
                table: "Abastecimento");

            migrationBuilder.DropColumn(
                name: "Litros",
                table: "Abastecimento");

            migrationBuilder.DropColumn(
                name: "NotaFiscal",
                table: "Abastecimento");

            migrationBuilder.DropColumn(
                name: "Odometro",
                table: "Abastecimento");

            migrationBuilder.DropColumn(
                name: "PostoId",
                table: "Abastecimento");

            migrationBuilder.DropColumn(
                name: "TipoCombustivelId",
                table: "Abastecimento");

            migrationBuilder.DropColumn(
                name: "ValorLitro",
                table: "Abastecimento");
        }
    }
}
