using Frota360.Application.Common;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Rotas.Commands.CalcularRota;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Rotas
{
    /// <summary>
    /// Fica em arquivo próprio, e não no <c>RotaHandlersTests</c>, porque a fixture é outra:
    /// este handler não toca repositório nenhum — só o cliente da Routes API.
    /// </summary>
    public class CalcularRotaHandlerTests
    {
        private readonly IGoogleRoutesClient _routesClient = Substitute.For<IGoogleRoutesClient>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public CalcularRotaHandlerTests() => _currentUser.EmpresaId.Returns(1);

        private CalcularRotaHandler CriarHandler() =>
            new(_routesClient, _currentUser, _auditoria, NullLogger<CalcularRotaHandler>.Instance);

        private static CalcularRotaRequest RequestValido() => new()
        {
            LatitudeOrigem = -25.4284m,
            LongitudeOrigem = -49.2733m,
            LatitudeDestino = -23.5505m,
            LongitudeDestino = -46.6333m
        };

        [Fact]
        public async Task Calcular_DeveRepassarAsCoordenadasEMapearResposta()
        {
            _routesClient.CalcularAsync(Arg.Any<Coordenada>(), Arg.Any<Coordenada>(), Arg.Any<CancellationToken>())
                .Returns(ResultadoCalculoRota.Ok(408_000, 18_000, "abc123"));

            var resposta = await CriarHandler().HandleAsync(new CalcularRotaCommand(RequestValido()));

            Assert.Equal(408_000, resposta.DistanciaMetros);
            Assert.Equal(18_000, resposta.DuracaoEstimadaSegundos);
            Assert.Equal("abc123", resposta.PolylineCodificada);

            await _routesClient.Received(1).CalcularAsync(
                Arg.Is<Coordenada>(c => c.Latitude == -25.4284m && c.Longitude == -49.2733m),
                Arg.Is<Coordenada>(c => c.Latitude == -23.5505m && c.Longitude == -46.6333m),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Calcular_DeveRegistrarAuditoriaDaChamadaCobrada()
        {
            _routesClient.CalcularAsync(Arg.Any<Coordenada>(), Arg.Any<Coordenada>(), Arg.Any<CancellationToken>())
                .Returns(ResultadoCalculoRota.Ok(408_000, 18_000, "abc123"));

            await CriarHandler().HandleAsync(new CalcularRotaCommand(RequestValido()));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Rota, AcoesAuditoria.Calculou, null,
                Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>?>());
        }

        [Fact]
        public async Task Calcular_FalhaDoServicoExterno_DeveVirar422SemVazarMotivoTecnico()
        {
            _routesClient.CalcularAsync(Arg.Any<Coordenada>(), Arg.Any<Coordenada>(), Arg.Any<CancellationToken>())
                .Returns(ResultadoCalculoRota.Falha("Routes API respondeu 403."));

            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarHandler().HandleAsync(new CalcularRotaCommand(RequestValido())));

            // A mensagem vai literalmente ao usuário: não pode carregar status HTTP da Google.
            Assert.DoesNotContain("403", excecao.Message);
        }

        [Fact]
        public async Task Calcular_FalhaDoServicoExterno_NaoDeveRegistrarAuditoria()
        {
            _routesClient.CalcularAsync(Arg.Any<Coordenada>(), Arg.Any<Coordenada>(), Arg.Any<CancellationToken>())
                .Returns(ResultadoCalculoRota.Falha("Tempo esgotado ao consultar a Routes API."));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CriarHandler().HandleAsync(new CalcularRotaCommand(RequestValido())));

            await _auditoria.DidNotReceiveWithAnyArgs().RegistrarAsync(default!, default!, default, default!);
        }
    }
}
