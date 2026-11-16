using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Frota360.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Frota360.Api.Tests
{
    public class ObservabilidadeTests
    {
        private const string CabecalhoDeAutenticacao = "Authorization=Basic%20dGVzdGU=";

        private static IConfiguration Configuracao(string? endpoint, string? cabecalhos = null) =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint,
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
                ["OTEL_EXPORTER_OTLP_HEADERS"] = cabecalhos,
                ["OTEL_SERVICE_NAME"] = "frota360-api",
            }).Build();

        private static ServiceProvider Provedor(IConfiguration configuracao, string ambiente)
        {
            var services = new ServiceCollection();
            services.AddSingleton(configuracao);
            services.AddLogging();
            services.AddObservabilidade(configuracao, ambiente);
            return services.BuildServiceProvider();
        }

        // Protobuf grava string como UTF-8 cru: basta procurar o texto no corpo.
        private static void ContemTexto(byte[] corpo, params string[] textos) =>
            Assert.All(textos, texto => Assert.True(
                corpo.AsSpan().IndexOf(Encoding.UTF8.GetBytes(texto)) >= 0, $"'{texto}' não está no corpo exportado"));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddObservabilidade_SemEndpoint_NaoRegistraTelemetria(string? endpoint)
        {
            using var provedor = Provedor(Configuracao(endpoint), "Development");

            Assert.False(Observabilidade.ExportacaoHabilitada(Configuracao(endpoint)));
            Assert.Null(provedor.GetService<TracerProvider>());
            Assert.Null(provedor.GetService<MeterProvider>());
        }

        [Fact]
        public async Task AddObservabilidade_ComEndpoint_DeveExportarTracesComAutenticacaoEIdentificacaoDoServico()
        {
            await using var coletor = await ColetorOtlp.IniciarAsync();

            using (var provedor = Provedor(Configuracao(coletor.Endpoint, CabecalhoDeAutenticacao), "Production"))
            {
                var rastreador = provedor.GetRequiredService<TracerProvider>();
                Assert.NotNull(provedor.GetService<MeterProvider>());

                using (var fonte = new ActivitySource("Microsoft.AspNetCore"))
                using (fonte.StartActivity("GET /api/v1/veiculo")) { }

                rastreador.ForceFlush();
            }

            var requisicao = await coletor.AguardarAsync("/v1/traces");

            Assert.Equal("Basic dGVzdGU=", requisicao.Autorizacao);
            ContemTexto(requisicao.Corpo, "service.name", "frota360-api", "deployment.environment.name", "Production", "GET /api/v1/veiculo");
        }

        [Fact]
        public async Task ExportarLogs_ComEndpoint_DeveEnviarOsLogsEstruturados()
        {
            await using var coletor = await ColetorOtlp.IniciarAsync();
            var configuracao = Configuracao(coletor.Endpoint, CabecalhoDeAutenticacao);

            using (var logger = new LoggerConfiguration().ExportarLogs(configuracao, "Production").CreateLogger())
                logger.Information("Veículo {Placa} cadastrado", "ABC1D23");

            var requisicao = await coletor.AguardarAsync("/v1/logs");

            Assert.Equal("Basic dGVzdGU=", requisicao.Autorizacao);
            ContemTexto(requisicao.Corpo, "service.name", "frota360-api", "deployment.environment.name", "Production", "Placa", "ABC1D23");
        }

        [Fact]
        public async Task ExportarLogs_NaoDeveExportarOEmailDeDesenvolvimentoComOToken()
        {
            await using var coletor = await ColetorOtlp.IniciarAsync();
            var configuracao = Configuracao(coletor.Endpoint, CabecalhoDeAutenticacao);

            using (var logger = new LoggerConfiguration().ExportarLogs(configuracao, "Production").CreateLogger())
            {
                logger.ForContext<LogEmailService>().Warning("[EMAIL DEV] {Corpo}", "https://app/convite?token=SEGREDO123");
                logger.Information("Rota {RotaId} encerrada", 42);
            }

            var requisicao = await coletor.AguardarAsync("/v1/logs");

            ContemTexto(requisicao.Corpo, "RotaId");
            Assert.True(requisicao.Corpo.AsSpan().IndexOf("SEGREDO123"u8) < 0, "o token do convite saiu da instância");
        }

        [Fact]
        public void ExportarLogs_SemEndpoint_DeveDevolverAMesmaConfiguracao()
        {
            var configuracao = new LoggerConfiguration();

            Assert.Same(configuracao, configuracao.ExportarLogs(Configuracao(null), "Development"));
        }

        [Theory]
        [InlineData("/health", false)]
        [InlineData("/health/detail", false)]
        [InlineData("/api/v1/veiculo", true)]
        [InlineData("/healthcheck", true)]
        public void DeveRastrear_DeveIgnorarSoAsVerificacoesDeSaude(string caminho, bool esperado)
        {
            var contexto = new DefaultHttpContext();
            contexto.Request.Path = caminho;

            Assert.Equal(esperado, Observabilidade.DeveRastrear(contexto));
        }

        [Theory]
        [InlineData("/health", 200, LogEventLevel.Verbose)]
        [InlineData("/health/detail", 200, LogEventLevel.Verbose)]
        [InlineData("/health", 503, LogEventLevel.Error)]
        [InlineData("/api/v1/veiculo", 200, LogEventLevel.Information)]
        [InlineData("/api/v1/veiculo", 422, LogEventLevel.Information)]
        [InlineData("/api/v1/veiculo", 500, LogEventLevel.Error)]
        public void NivelDoLogDeRequisicao_DeveSilenciarSoOHealthSaudavel(string caminho, int status, LogEventLevel esperado)
        {
            var contexto = new DefaultHttpContext();
            contexto.Request.Path = caminho;
            contexto.Response.StatusCode = status;

            Assert.Equal(esperado, Observabilidade.NivelDoLogDeRequisicao(contexto, 1, null));
        }

        [Fact]
        public void NivelDoLogDeRequisicao_ComExcecao_DeveSerErro()
        {
            var contexto = new DefaultHttpContext();
            contexto.Request.Path = "/health";

            Assert.Equal(LogEventLevel.Error, Observabilidade.NivelDoLogDeRequisicao(contexto, 1, new InvalidOperationException()));
        }

        private sealed record RequisicaoOtlp(string Caminho, string Autorizacao, byte[] Corpo);

        /// <summary>Receptor OTLP/HTTP mínimo, numa porta livre, que guarda o que recebe.</summary>
        private sealed class ColetorOtlp(WebApplication app, ConcurrentQueue<RequisicaoOtlp> recebidas) : IAsyncDisposable
        {
            public string Endpoint => app.Urls.First();

            public static async Task<ColetorOtlp> IniciarAsync()
            {
                var builder = WebApplication.CreateSlimBuilder();
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                builder.Configuration["AllowedHosts"] = "*";
                builder.Logging.ClearProviders();
                var app = builder.Build();
                var recebidas = new ConcurrentQueue<RequisicaoOtlp>();

                app.Run(async contexto =>
                {
                    using var corpo = new MemoryStream();
                    await contexto.Request.Body.CopyToAsync(corpo);
                    recebidas.Enqueue(new(contexto.Request.Path, contexto.Request.Headers.Authorization.ToString(), corpo.ToArray()));
                });

                await app.StartAsync();
                return new ColetorOtlp(app, recebidas);
            }

            public async Task<RequisicaoOtlp> AguardarAsync(string caminho)
            {
                var limite = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < limite)
                {
                    var requisicao = recebidas.FirstOrDefault(r => r.Caminho == caminho);
                    if (requisicao is not null)
                        return requisicao;
                    await Task.Delay(50);
                }

                throw new TimeoutException($"Nada chegou em {Endpoint}{caminho}. Recebido: {string.Join(", ", recebidas.Select(r => r.Caminho))}");
            }

            public async ValueTask DisposeAsync()
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }
}
