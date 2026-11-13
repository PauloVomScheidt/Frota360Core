using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TracadoDeRotaGoogleRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataCalculoRota",
                table: "Rota",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistanciaMetros",
                table: "Rota",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DuracaoEstimadaSegundos",
                table: "Rota",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoChegada",
                table: "Rota",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnderecoPartida",
                table: "Rota",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "LatitudeChegada",
                table: "Rota",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LatitudePartida",
                table: "Rota",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LongitudeChegada",
                table: "Rota",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LongitudePartida",
                table: "Rota",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PolylineCodificada",
                table: "Rota",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataCalculoRota",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "DistanciaMetros",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "DuracaoEstimadaSegundos",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "EnderecoChegada",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "EnderecoPartida",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "LatitudeChegada",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "LatitudePartida",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "LongitudeChegada",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "LongitudePartida",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "PolylineCodificada",
                table: "Rota");
        }
    }
}
