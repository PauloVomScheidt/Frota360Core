using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Images;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Frota360.IntegrationTests
{
    internal static class ConfiguracaoDoTestcontainers
    {
        /// <summary>
        /// Desliga o resource reaper (o container "ryuk" que o Testcontainers sobe para
        /// limpar recursos órfãos). No Docker Desktop em Windows ele falha ao ser baixado
        /// pelo named pipe e derruba a suíte inteira antes do primeiro teste.
        ///
        /// Abrir mão dele é seguro aqui porque a <see cref="BancoFixture"/> descarta o
        /// container no <c>DisposeAsync</c>. O caso que o ryuk cobriria é o processo de teste
        /// ser morto no meio (Ctrl+C, crash), que deixaria um container de pé — visível em
        /// <c>docker ps</c> e removível com <c>docker rm</c>.
        /// </summary>
        [ModuleInitializer]
        internal static void Inicializar() =>
            Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }

    /// <summary>
    /// Sobe um PostgreSQL descartável por execução e aplica as migrations reais.
    ///
    /// É <b>descartável de propósito</b>: não usa o container `pg-frota360` de
    /// desenvolvimento nem a connection string dos appsettings, então rodar os testes nunca
    /// mexe nos dados de quem está desenvolvendo.
    ///
    /// Usa <c>MigrateAsync</c>, e não <c>EnsureCreatedAsync</c>: o segundo cria o schema a
    /// partir do modelo e <b>pularia as migrations</b> — justamente o artefato que precisa
    /// ser exercitado. Assim, uma migration quebrada falha aqui e não no deploy.
    ///
    /// Exige Docker no ar, que já é pré-requisito do projeto.
    /// </summary>
    public sealed class BancoFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .WithDatabase("frota360_testes")
            // O fuso do banco é irrelevante para as colunas `timestamp without time zone`,
            // mas fixá-lo evita que a máquina de quem roda os testes influencie o resultado.
            .WithEnvironment("TZ", "America/Sao_Paulo")
            // A imagem já está local (é a mesma do compose de desenvolvimento). Evitar o pull
            // tira a rede do caminho e faz a suíte subir em segundos.
            .WithImagePullPolicy(PullPolicy.Missing)
            .Build();

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            await using var contexto = CriarContexto();
            await contexto.Database.MigrateAsync();
        }

        public async Task DisposeAsync() => await _container.DisposeAsync();

        /// <summary>
        /// Um contexto novo por uso. Cada teste que precisa reler do banco deve pedir um
        /// contexto limpo, senão o change tracker devolve a instância em memória e o teste
        /// passa sem ter provado nada sobre o que foi realmente gravado.
        /// </summary>
        public Frota360DbContext CriarContexto() =>
            new(new DbContextOptionsBuilder<Frota360DbContext>()
                .UseNpgsql(ConnectionString)
                .Options);
    }

    [CollectionDefinition(Nome)]
    public sealed class BancoCollection : ICollectionFixture<BancoFixture>
    {
        public const string Nome = "banco";
    }

    /// <summary>
    /// Gerador de valores únicos <b>compartilhado por todas as classes de teste</b>.
    ///
    /// O contador precisa ser um só: as classes dividem o mesmo banco, e um contador por
    /// classe faz duas delas produzirem o mesmo <c>mot1@x.com</c> e esbarrarem no índice
    /// único de <c>Usuario.Email</c> — um teste quebrando por causa de outro, sem relação
    /// com o que ele deveria provar.
    /// </summary>
    internal static class Unicos
    {
        private static int _sequencia;

        private static int Proximo() => Interlocked.Increment(ref _sequencia);

        public static string Texto(string prefixo) => $"{prefixo} {Proximo()}";

        public static string Email(string prefixo) => $"{prefixo}{Proximo()}@teste.com";

        /// <summary>Sete caracteres, o tamanho real de uma placa Mercosul.</summary>
        public static string Placa() => $"T{Proximo():D6}";
    }
}
