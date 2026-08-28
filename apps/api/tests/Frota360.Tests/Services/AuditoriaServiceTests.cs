using Frota360.Application.Common;
using Frota360.Application.Interfaces;
using Frota360.Application.Services;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;

namespace Frota360.Tests.Services
{
    public class AuditoriaServiceTests
    {
        private readonly ILogAuditoriaRepository _repository = Substitute.For<ILogAuditoriaRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

        public AuditoriaServiceTests()
        {
            _currentUser.EmpresaId.Returns(1);
            _currentUser.UsuarioId.Returns(10);
            _currentUser.Nome.Returns("Ana Souza");
            _currentUser.Email.Returns("ana@empresa.com");
            _currentUser.Role.Returns(Roles.Admin);
            _currentUser.IpOrigem.Returns("203.0.113.7");
        }

        private AuditoriaService CriarServico() =>
            new(_repository, _currentUser, NullLogger<AuditoriaService>.Instance);

        [Fact]
        public async Task Registrar_DeveGravarEscopadoNaEmpresaComOAtorDaClaim()
        {
            var service = CriarServico();

            await service.RegistrarAsync(EntidadesAuditadas.Veiculo, AcoesAuditoria.Criou, 7,
                "Cadastrou o veículo ABC1D23 (Fiat Strada)");

            await _repository.Received(1).AddAsync(Arg.Is<LogAuditoria>(l =>
                l.EmpresaId == 1
                && l.UsuarioId == 10
                && l.UsuarioNome == "Ana Souza"
                && l.UsuarioEmail == "ana@empresa.com"
                && l.UsuarioRole == Roles.Admin
                && l.Entidade == EntidadesAuditadas.Veiculo
                && l.Acao == AcoesAuditoria.Criou
                && l.EntidadeId == 7
                && l.IpOrigem == "203.0.113.7"
                && l.DataHora != default));
        }

        [Fact]
        public async Task Registrar_SemAlteracoes_DeveGravarAlteracoesNulo()
        {
            var service = CriarServico();

            await service.RegistrarAsync(EntidadesAuditadas.Rota, AcoesAuditoria.Excluiu, 3, "Excluiu a rota #3");

            await _repository.Received(1).AddAsync(Arg.Is<LogAuditoria>(l => l.Alteracoes == null));
        }

        [Fact]
        public async Task Registrar_ComAlteracoes_DeveSerializarODiffEmJson()
        {
            var service = CriarServico();
            LogAuditoria? gravado = null;
            await _repository.AddAsync(Arg.Do<LogAuditoria>(l => gravado = l));

            var alteracoes = new AlteracoesBuilder()
                .Comparar("Placa", "ABC1D23", "XYZ9K87")
                .Comparar("Quilometragem", 50_000, 50_000) // igual: não entra no diff
                .Construir();

            await service.RegistrarAsync(EntidadesAuditadas.Veiculo, AcoesAuditoria.Atualizou, 7,
                "Atualizou o veículo ABC1D23", alteracoes);

            Assert.NotNull(gravado?.Alteracoes);

            var diff = JsonSerializer.Deserialize<List<AlteracaoCampo>>(gravado!.Alteracoes!);
            var unica = Assert.Single(diff!);
            Assert.Equal("Placa", unica.Campo);
            Assert.Equal("ABC1D23", unica.De);
            Assert.Equal("XYZ9K87", unica.Para);
        }

        [Fact]
        public async Task RegistrarComo_DeveUsarOAtorInformadoEmVezDaClaim()
        {
            var service = CriarServico();

            // Aceite de convite: requisição anônima, ator recém-criado.
            var novo = new Usuario
            {
                Id = 42,
                EmpresaId = 8,
                Nome = "João Lima",
                Email = "joao@empresa.com",
                Role = Roles.Operador
            };

            await service.RegistrarComoAsync(novo.EmpresaId, novo,
                EntidadesAuditadas.Convite, AcoesAuditoria.Aceitou, 5,
                "Aceitou o convite e criou a conta joao@empresa.com como Operador");

            await _repository.Received(1).AddAsync(Arg.Is<LogAuditoria>(l =>
                l.EmpresaId == 8
                && l.UsuarioId == 42
                && l.UsuarioNome == "João Lima"
                && l.UsuarioRole == Roles.Operador));
        }

        /// <summary>
        /// A garantia central do desenho: a auditoria roda depois de a operação de negócio já
        /// ter sido persistida, então uma falha aqui não pode virar 500 numa edição bem-sucedida.
        /// </summary>
        [Fact]
        public async Task Registrar_QuandoORepositorioFalha_NaoDevePropagarAExcecao()
        {
            _repository.AddAsync(Arg.Any<LogAuditoria>())
                .ThrowsAsync(new InvalidOperationException("banco fora do ar"));

            var service = CriarServico();

            await service.RegistrarAsync(EntidadesAuditadas.Veiculo, AcoesAuditoria.Criou, 1, "Cadastrou um veículo");
        }
    }
}
