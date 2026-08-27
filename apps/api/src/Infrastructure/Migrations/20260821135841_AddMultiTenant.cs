using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Empresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CNPJ = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresa", x => x.Id);
                });

            // Empresa padrão (Id 1): recebe os dados pré-existentes e os registros públicos
            // até o fluxo de convites (fase 3 do PLANO-AUTH-ROLES.md) entrar no lugar.
            migrationBuilder.InsertData(
                table: "Empresa",
                columns: new[] { "Id", "Nome", "Ativo", "DataInclusao" },
                values: new object[] { 1, "Empresa Padrão", true, new DateTime(2026, 8, 21) });

            migrationBuilder.DropIndex(
                name: "IX_Motorista_CPF",
                table: "Motorista");

            migrationBuilder.DropIndex(
                name: "IX_Motorista_Email",
                table: "Motorista");

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Veiculo",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Usuario",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Rota",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Motorista",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Veiculo_EmpresaId",
                table: "Veiculo",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_EmpresaId",
                table: "Usuario",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rota_EmpresaId",
                table: "Rota",
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

            migrationBuilder.CreateIndex(
                name: "IX_Empresa_CNPJ",
                table: "Empresa",
                column: "CNPJ",
                unique: true,
                filter: "[CNPJ] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Motorista_Empresa_EmpresaId",
                table: "Motorista",
                column: "EmpresaId",
                principalTable: "Empresa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rota_Empresa_EmpresaId",
                table: "Rota",
                column: "EmpresaId",
                principalTable: "Empresa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuario_Empresa_EmpresaId",
                table: "Usuario",
                column: "EmpresaId",
                principalTable: "Empresa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Veiculo_Empresa_EmpresaId",
                table: "Veiculo",
                column: "EmpresaId",
                principalTable: "Empresa",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Motorista_Empresa_EmpresaId",
                table: "Motorista");

            migrationBuilder.DropForeignKey(
                name: "FK_Rota_Empresa_EmpresaId",
                table: "Rota");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuario_Empresa_EmpresaId",
                table: "Usuario");

            migrationBuilder.DropForeignKey(
                name: "FK_Veiculo_Empresa_EmpresaId",
                table: "Veiculo");

            migrationBuilder.DropTable(
                name: "Empresa");

            migrationBuilder.DropIndex(
                name: "IX_Veiculo_EmpresaId",
                table: "Veiculo");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_EmpresaId",
                table: "Usuario");

            migrationBuilder.DropIndex(
                name: "IX_Rota_EmpresaId",
                table: "Rota");

            migrationBuilder.DropIndex(
                name: "IX_Motorista_EmpresaId_CPF",
                table: "Motorista");

            migrationBuilder.DropIndex(
                name: "IX_Motorista_EmpresaId_Email",
                table: "Motorista");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Veiculo");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Usuario");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Motorista");

            migrationBuilder.CreateIndex(
                name: "IX_Motorista_CPF",
                table: "Motorista",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Motorista_Email",
                table: "Motorista",
                column: "Email",
                unique: true);
        }
    }
}
