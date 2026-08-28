using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MotoristaComoRoleDeUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ATENÇÃO — PERDA DE DADOS DELIBERADA.
            // Rota.CodigoMotorista deixa de apontar para Motorista e passa a apontar para
            // Usuario. Os ids gravados são de uma tabela que está sendo destruída e não
            // têm correspondência em Usuario, então a FK nova não teria como validá-los.
            // Acordado por ser base de desenvolvimento: se este repositório ganhar um
            // ambiente com dados reais, esta migration NÃO pode rodar como está — o
            // caminho ali seria remapear por e-mail antes de trocar a FK.
            migrationBuilder.Sql("DELETE FROM Rota");

            // O nome da constraint divergiu do histórico de migrations em algum ponto
            // (no banco ela é FK_Rota_Motorista, não FK_Rota_Motorista_CodigoMotorista),
            // então derrubamos pelo catálogo em vez de pelo nome.
            migrationBuilder.Sql(@"
                DECLARE @fk sysname;
                SELECT @fk = name FROM sys.foreign_keys
                 WHERE parent_object_id = OBJECT_ID('Rota')
                   AND referenced_object_id = OBJECT_ID('Motorista');
                IF @fk IS NOT NULL EXEC('ALTER TABLE [Rota] DROP CONSTRAINT [' + @fk + ']');");

            migrationBuilder.DropTable(
                name: "Motorista");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_EmpresaId",
                table: "Usuario");

            migrationBuilder.AddColumn<string>(
                name: "CPF",
                table: "Usuario",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataNascimento",
                table: "Usuario",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_EmpresaId_CPF",
                table: "Usuario",
                columns: new[] { "EmpresaId", "CPF" },
                unique: true,
                filter: "[CPF] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Rota_Usuario_CodigoMotorista",
                table: "Rota",
                column: "CodigoMotorista",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rota_Usuario_CodigoMotorista",
                table: "Rota");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_EmpresaId_CPF",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "CPF",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "DataNascimento",
                table: "Usuario");

            migrationBuilder.CreateTable(
                name: "Motorista",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CPF = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DataNascimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Motorista", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Motorista_Empresa_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_EmpresaId",
                table: "Usuario",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Motorista_EmpresaId_CPF",
                table: "Motorista",
                columns: new[] { "EmpresaId", "CPF" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Motorista_EmpresaId_Email",
                table: "Motorista",
                columns: new[] { "EmpresaId", "Email" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rota_Motorista_CodigoMotorista",
                table: "Rota",
                column: "CodigoMotorista",
                principalTable: "Motorista",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
