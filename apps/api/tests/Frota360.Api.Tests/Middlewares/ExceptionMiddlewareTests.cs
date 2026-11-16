using Frota360.Api.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using System.Text.Json;

namespace Frota360.Api.Tests.Middlewares
{
    public class ExceptionMiddlewareTests
    {
        private static async Task<(int Status, JsonElement Corpo)> Executar(RequestDelegate proximo)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var middleware = new ExceptionMiddleware(proximo, NullLogger<ExceptionMiddleware>.Instance);
            await middleware.InvokeAsync(context);

            context.Response.Body.Position = 0;
            var corpo = await JsonDocument.ParseAsync(context.Response.Body);
            return (context.Response.StatusCode, corpo.RootElement);
        }

        public static TheoryData<Exception, int, string> Mapeamentos => new()
        {
            { new InvalidOperationException("Veículo já está em rota."), 422, "Veículo já está em rota." },
            { new KeyNotFoundException("Rota não encontrada."), 404, "Rota não encontrada." },
            { new ArgumentNullException("request"), 400, "Requisição inválida." },
            { new UnauthorizedAccessException("detalhe interno"), 401, "Não autorizado." },
            { new NullReferenceException("detalhe interno"), 500, "Ocorreu um erro interno. Tente novamente mais tarde." },
            { BancoFora(new NpgsqlException("Failed to connect to 127.0.0.1:5432")), 503, Indisponivel },
            { BancoFora(new TimeoutException("Timeout during reading attempt")), 503, Indisponivel },
            { BancoFora(new NpgsqlException("externa", new TimeoutException())), 503, Indisponivel },
            { new NpgsqlException("Failed to connect to 127.0.0.1:5432"), 503, Indisponivel },
        };

        private const string Indisponivel = "Serviço temporariamente indisponível. Tente novamente em instantes.";

        // É como o NpgsqlExecutionStrategy entrega a queda do banco: uma InvalidOperationException,
        // o mesmo tipo das regras de negócio, com a falha de conexão por dentro.
        private static InvalidOperationException BancoFora(Exception causa) =>
            new("An exception has been raised that is likely due to a transient failure.", causa);

        [Fact]
        public async Task InvokeAsync_RegraDeNegocioComCausaQueNaoEInfraestrutura_DeveContinuar422()
        {
            var excecao = new InvalidOperationException("Veículo já está em rota.", new ArgumentException("detalhe"));

            var (status, corpo) = await Executar(_ => throw excecao);

            Assert.Equal(422, status);
            Assert.Equal("Veículo já está em rota.", corpo.GetProperty("mensagem").GetString());
        }

        [Theory]
        [MemberData(nameof(Mapeamentos))]
        public async Task InvokeAsync_Excecao_DeveResponderStatusEMensagemNoEnvelope(Exception excecao, int status, string mensagem)
        {
            var (statusRecebido, corpo) = await Executar(_ => throw excecao);

            Assert.Equal(status, statusRecebido);
            Assert.False(corpo.GetProperty("sucesso").GetBoolean());
            Assert.Equal(mensagem, corpo.GetProperty("mensagem").GetString());
        }

        [Fact]
        public async Task InvokeAsync_SemExcecao_DeveDeixarARespostaDoProximo()
        {
            var context = new DefaultHttpContext();
            var middleware = new ExceptionMiddleware(
                ctx => { ctx.Response.StatusCode = StatusCodes.Status204NoContent; return Task.CompletedTask; },
                NullLogger<ExceptionMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        }
    }
}
