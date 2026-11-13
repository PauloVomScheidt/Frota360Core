using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Frota360.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnderecoDeRotaSubstituiOrigemDestino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copia antes de derrubar: sem isto o DROP apagaria o endereço de toda rota
            // anterior à integração — a TracadoDeRotaGoogleRoutes criou EnderecoPartida/Chegada
            // com default '', então todas as linhas existentes chegam aqui vazias.
            // O WHERE protege a reexecução e as linhas que já tenham endereço do Places.
            migrationBuilder.Sql("""
                UPDATE "Rota" SET "EnderecoPartida" = "Origem"  WHERE "EnderecoPartida" = '';
                UPDATE "Rota" SET "EnderecoChegada" = "Destino" WHERE "EnderecoChegada" = '';
                """);

            migrationBuilder.DropColumn(
                name: "Destino",
                table: "Rota");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "Rota");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Destino",
                table: "Rota",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "Rota",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Volta o que der: as colunas antigas são menores (100/150) que os 250 do
            // endereço formatado, então um endereço longo do Places é truncado na descida.
            migrationBuilder.Sql("""
                UPDATE "Rota" SET "Origem"  = LEFT("EnderecoPartida", 100),
                                 "Destino" = LEFT("EnderecoChegada", 150);
                """);
        }
    }
}
