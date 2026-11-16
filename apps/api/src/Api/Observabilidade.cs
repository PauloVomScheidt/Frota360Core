using Frota360.Infrastructure.Services;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace Frota360.Api
{
    /// <summary>
    /// Traces, métricas e logs exportados por OTLP. Tudo é lido das variáveis padrão do
    /// OpenTelemetry (OTEL_EXPORTER_OTLP_ENDPOINT, _PROTOCOL, _HEADERS, OTEL_SERVICE_NAME), as
    /// mesmas para o SDK e para o sink do Serilog: trocar de destino (Grafana local, Grafana
    /// Cloud) é só trocar o endpoint. Sem endpoint nada é registrado.
    /// </summary>
    public static class Observabilidade
    {
        private const string ChaveDoEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
        private const string AtributoDeAmbiente = "deployment.environment.name";

        public static bool ExportacaoHabilitada(IConfiguration configuracao) =>
            !string.IsNullOrWhiteSpace(configuracao[ChaveDoEndpoint]);

        public static IServiceCollection AddObservabilidade(this IServiceCollection services, IConfiguration configuracao, string ambiente)
        {
            if (!ExportacaoHabilitada(configuracao))
                return services;

            services.AddOpenTelemetry()
                .ConfigureResource(recurso => recurso.AddAttributes([new(AtributoDeAmbiente, ambiente)]))
                .WithTracing(traces => traces
                    .AddAspNetCoreInstrumentation(opcoes => opcoes.Filter = DeveRastrear)
                    .AddHttpClientInstrumentation()
                    .AddNpgsql())
                .WithMetrics(metricas => metricas
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddNpgsqlInstrumentation())
                .UseOtlpExporter();

            return services;
        }

        public static LoggerConfiguration ExportarLogs(this LoggerConfiguration log, IConfiguration configuracao, string ambiente)
        {
            if (!ExportacaoHabilitada(configuracao))
                return log;

            // O LogEmailService imprime o link de convite/reset com o token. Em produção ele só roda
            // com o Resend mal configurado, e mesmo assim o token não pode sair da instância.
            return log.WriteTo.Logger(otlp => otlp
                .Filter.ByExcluding(Matching.FromSource<LogEmailService>())
                .WriteTo.OpenTelemetry(
                    opcoes => opcoes.ResourceAttributes = new Dictionary<string, object> { [AtributoDeAmbiente] = ambiente },
                    chave => configuracao[chave]!));
        }

        // O /health é consultado pelo HEALTHCHECK do Docker (30 s) e pelo monitor externo (1 min):
        // rastreado, ele afogaria os traces de verdade.
        public static bool DeveRastrear(HttpContext contexto) => !EhVerificacaoDeSaude(contexto.Request.Path);

        public static LogEventLevel NivelDoLogDeRequisicao(HttpContext contexto, double _, Exception? erro)
        {
            if (erro is not null || contexto.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                return LogEventLevel.Error;

            return EhVerificacaoDeSaude(contexto.Request.Path) ? LogEventLevel.Verbose : LogEventLevel.Information;
        }

        private static bool EhVerificacaoDeSaude(PathString caminho) => caminho.StartsWithSegments("/health");
    }
}
