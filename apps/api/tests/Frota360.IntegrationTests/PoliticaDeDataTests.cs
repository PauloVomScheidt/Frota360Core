using Frota360.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frota360.IntegrationTests
{
    /// <summary>
    /// O teste mais importante do projeto: prova que os dois <c>DateTimeKind</c> que o sistema
    /// produz convivem na mesma coluna.
    ///
    /// O Npgsql recusa <c>Kind=Utc</c> numa coluna sem fuso e <c>Kind=Unspecified</c> numa com
    /// fuso, lançando em vez de converter. O sistema grava <c>Kind=Local</c> (todo
    /// <c>DateTime.Now</c>) e <c>Kind=Unspecified</c> (o <c>"aaaa-MM-dd"</c> que vem do front)
    /// nos mesmos campos — <c>EncerrarRotaHandler</c> faz os dois na mesma propriedade.
    /// Sem o <c>DataSemFusoConverter</c>, um dos caminhos quebraria em produção.
    /// </summary>
    [Collection(BancoCollection.Nome)]
    public class PoliticaDeDataTests(BancoFixture fixture)
    {
        [Theory]
        [InlineData(DateTimeKind.Local)]        // DateTime.Now — todo handler de escrita
        [InlineData(DateTimeKind.Unspecified)]  // JSON do front, "aaaa-MM-dd"
        [InlineData(DateTimeKind.Utc)]          // regressão: se alguém reintroduzir UtcNow
        public async Task Gravar_QualquerKind_DevePersistirSemLancar(DateTimeKind kind)
        {
            var quando = DateTime.SpecifyKind(new DateTime(2026, 8, 30, 14, 30, 0), kind);

            await using var contexto = fixture.CriarContexto();
            var empresa = new Empresa { Nome = $"Kind {kind}", DataInclusao = quando };
            contexto.Empresas.Add(empresa);

            await contexto.SaveChangesAsync();

            Assert.True(empresa.Id > 0);
        }

        [Fact]
        public async Task LerAposGravar_DevePreservarORelogioDeParedeEDevolverUnspecified()
        {
            // O relógio de parede tem de sobreviver intacto: é ele que o front exibe verbatim.
            // Se algum dia isto voltar com Kind=Utc ou com as horas deslocadas, toda tela com
            // data passa a mentir.
            var local = DateTime.SpecifyKind(new DateTime(2026, 8, 30, 14, 30, 0), DateTimeKind.Local);

            int id;
            await using (var escrita = fixture.CriarContexto())
            {
                var empresa = new Empresa { Nome = "Relogio de parede", DataInclusao = local };
                escrita.Empresas.Add(empresa);
                await escrita.SaveChangesAsync();
                id = empresa.Id;
            }

            await using var leitura = fixture.CriarContexto();
            var lida = await leitura.Empresas.AsNoTracking().SingleAsync(e => e.Id == id);

            Assert.Equal(new DateTime(2026, 8, 30, 14, 30, 0), lida.DataInclusao);
            Assert.Equal(DateTimeKind.Unspecified, lida.DataInclusao.Kind);
        }

        [Fact]
        public async Task GravarOsDoisKinds_NaMesmaColuna_DeveConviver()
        {
            // Reproduz o EncerrarRotaHandler: `request.DataFim ?? DateTime.Now` põe Unspecified
            // (data do front) ou Local (agora) na MESMA propriedade, conforme o caminho.
            var doFront = DateTime.SpecifyKind(new DateTime(2026, 8, 30), DateTimeKind.Unspecified);
            var doServidor = DateTime.SpecifyKind(new DateTime(2026, 8, 30, 17, 45, 0), DateTimeKind.Local);

            await using var contexto = fixture.CriarContexto();
            contexto.Empresas.AddRange(
                new Empresa { Nome = "Veio do front", DataInclusao = doFront },
                new Empresa { Nome = "Veio do servidor", DataInclusao = doServidor });

            await contexto.SaveChangesAsync();

            var nomes = new[] { "Veio do front", "Veio do servidor" };
            var gravadas = await contexto.Empresas.AsNoTracking()
                .Where(e => nomes.Contains(e.Nome))
                .OrderBy(e => e.Nome)
                .ToListAsync();

            Assert.Equal(2, gravadas.Count);
            Assert.Equal(new DateTime(2026, 8, 30), gravadas[0].DataInclusao);
            Assert.Equal(new DateTime(2026, 8, 30, 17, 45, 0), gravadas[1].DataInclusao);
            Assert.All(gravadas, e => Assert.Equal(DateTimeKind.Unspecified, e.DataInclusao.Kind));
        }

        [Fact]
        public async Task ColunasDeData_DevemSerTimestampSemFuso()
        {
            // Se alguém trocar o mapeamento para timestamptz, o converter para de funcionar e
            // a API passa a serializar com sufixo Z — quebrando a exibição no front em um dia.
            await using var contexto = fixture.CriarContexto();

            // O EF projeta SqlQuery<T> sobre uma coluna chamada "Value" — daí o alias.
            var tipo = await contexto.Database
                .SqlQuery<string>($@"SELECT data_type AS ""Value"" FROM information_schema.columns
                                     WHERE table_name = 'Empresa' AND column_name = 'DataInclusao'")
                .SingleAsync();

            Assert.Equal("timestamp without time zone", tipo);
        }
    }
}
