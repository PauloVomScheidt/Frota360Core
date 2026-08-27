using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHodometroRota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KmFinal",
                table: "Rota",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KmInicial",
                table: "Rota",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KmPercorrido",
                table: "Rota",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KmFinal",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "KmInicial",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "KmPercorrido",
                table: "Rota");
        }
    }
}
