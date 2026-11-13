using Frota360.Application.Common;
using Frota360.Application.DTOs.TipoCombustivel.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.TiposCombustivel.Commands.CreateTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Commands.DeleteTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Commands.UpdateTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Queries.GetAllTiposCombustivel;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.TiposCombustivel
{
    public class TipoCombustivelHandlersTests
    {
        private readonly ITipoCombustivelRepository _repository = Substitute.For<ITipoCombustivelRepository>();
        private readonly IAbastecimentoRepository _abastecimentoRepository = Substitute.For<IAbastecimentoRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public TipoCombustivelHandlersTests() => _currentUser.EmpresaId.Returns(1);

        private CreateTipoCombustivelHandler CriarCreateHandler() =>
            new(_repository, _currentUser, _auditoria, NullLogger<CreateTipoCombustivelHandler>.Instance);

        private UpdateTipoCombustivelHandler CriarUpdateHandler() =>
            new(_repository, _currentUser, _auditoria, NullLogger<UpdateTipoCombustivelHandler>.Instance);

        private DeleteTipoCombustivelHandler CriarDeleteHandler() =>
            new(_repository, _abastecimentoRepository, _currentUser, _auditoria,
                NullLogger<DeleteTipoCombustivelHandler>.Instance);

        private GetAllTiposCombustivelHandler CriarGetAllHandler() =>
            new(_repository, _currentUser, NullLogger<GetAllTiposCombustivelHandler>.Instance);

        private static TipoCombustivel NovoTipo(int id = 1, string nome = "Diesel S10", bool ativo = true) =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Ativo = ativo };

        [Fact]
        public async Task Create_DevePersistirEscopadoNaEmpresaEMapearResposta()
        {
            _repository.AddAsync(Arg.Any<TipoCombustivel>()).Returns(c => c.Arg<TipoCombustivel>());

            var handler = CriarCreateHandler();
            var resposta = await handler.HandleAsync(
                new CreateTipoCombustivelCommand(new CreateTipoCombustivelRequest { Nome = "  Diesel S10  " }));

            await _repository.Received(1).AddAsync(Arg.Is<TipoCombustivel>(t => t.EmpresaId == 1));
            // O nome é gravado sem espaços nas pontas — senão " Pedágio" e "Diesel S10"
            // conviveriam apesar do índice único.
            Assert.Equal("Diesel S10", resposta.Nome);
            Assert.True(resposta.Ativo);
        }

        [Fact]
        public async Task Create_ComNomeJaUsadoNaEmpresa_DeveRecusar()
        {
            _repository.ExisteNomeAsync(1, "Diesel S10", null).Returns(true);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateTipoCombustivelCommand(
                    new CreateTipoCombustivelRequest { Nome = "Diesel S10" })));
        }

        [Fact]
        public async Task Create_DeveRegistrarAuditoria()
        {
            _repository.AddAsync(Arg.Any<TipoCombustivel>()).Returns(c => c.Arg<TipoCombustivel>());

            var handler = CriarCreateHandler();
            await handler.HandleAsync(new CreateTipoCombustivelCommand(
                new CreateTipoCombustivelRequest { Nome = "Diesel S10" }));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.TipoCombustivel, AcoesAuditoria.Criou,
                Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>?>());
        }

        [Fact]
        public async Task Update_DeveEscoparNaEmpresaERegistrarODiff()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _repository.UpdateAsync(Arg.Any<TipoCombustivel>()).Returns(c => c.Arg<TipoCombustivel>());

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdateTipoCombustivelCommand(1,
                new UpdateTipoCombustivelRequest { Nome = "Diesel S10 aditivado", Ativo = false }));

            await _repository.Received(1).GetByIdAsync(1, 1);
            Assert.NotNull(resposta);
            Assert.False(resposta.Ativo);

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.TipoCombustivel, AcoesAuditoria.Atualizou, 1, Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>?>(a => a != null && a.Any(c => c.Campo == "Nome")));
        }

        [Fact]
        public async Task Update_TipoInexistente_DeveDevolverNulo()
        {
            _repository.GetByIdAsync(1, 1).Returns((TipoCombustivel?)null);

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdateTipoCombustivelCommand(1,
                new UpdateTipoCombustivelRequest { Nome = "Diesel S10" }));

            Assert.Null(resposta);
        }

        [Fact]
        public async Task Delete_ComTipoEmUso_DeveRecusarPedindoParaInativar()
        {
            // Apagar o tipo levaria junto o nome que identifica o histórico de abastecimento.
            _repository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _abastecimentoRepository.ExisteComTipoCombustivelAsync(1, 1).Returns(true);

            var handler = CriarDeleteHandler();

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new DeleteTipoCombustivelCommand(1)));

            Assert.Contains("Inative-o", erro.Message);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<TipoCombustivel>());
        }

        [Fact]
        public async Task Delete_ComTipoSemUso_DeveRemoverERegistrarAuditoria()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _abastecimentoRepository.ExisteComTipoCombustivelAsync(1, 1).Returns(false);

            var handler = CriarDeleteHandler();
            var removido = await handler.HandleAsync(new DeleteTipoCombustivelCommand(1));

            Assert.True(removido);
            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.TipoCombustivel, AcoesAuditoria.Excluiu, 1, Arg.Any<string>(),
                Arg.Any<IEnumerable<AlteracaoCampo>?>());
        }

        [Fact]
        public async Task GetAll_DeveEscoparNaEmpresaERepassarApenasAtivos()
        {
            _repository.GetAllAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns([NovoTipo()]);

            var handler = CriarGetAllHandler();
            await handler.HandleAsync(new GetAllTiposCombustivelQuery(ApenasAtivos: true));

            await _repository.Received(1).GetAllAsync(1, true);
        }
    }
}
