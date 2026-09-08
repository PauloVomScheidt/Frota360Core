using Frota360.Application.Common;
using Frota360.Application.DTOs.TipoDespesa.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.TiposDespesa.Commands.CreateTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Commands.DeleteTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Commands.UpdateTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Queries.GetAllTiposDespesa;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.TiposDespesa
{
    public class TipoDespesaHandlersTests
    {
        private readonly ITipoDespesaRepository _repository = Substitute.For<ITipoDespesaRepository>();
        private readonly IDespesaRepository _despesaRepository = Substitute.For<IDespesaRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public TipoDespesaHandlersTests() => _currentUser.EmpresaId.Returns(1);

        private CreateTipoDespesaHandler CriarCreateHandler() =>
            new(_repository, _currentUser, _auditoria, NullLogger<CreateTipoDespesaHandler>.Instance);

        private UpdateTipoDespesaHandler CriarUpdateHandler() =>
            new(_repository, _currentUser, _auditoria, NullLogger<UpdateTipoDespesaHandler>.Instance);

        private DeleteTipoDespesaHandler CriarDeleteHandler() =>
            new(_repository, _despesaRepository, _currentUser, _auditoria,
                NullLogger<DeleteTipoDespesaHandler>.Instance);

        private GetAllTiposDespesaHandler CriarGetAllHandler() =>
            new(_repository, _currentUser, NullLogger<GetAllTiposDespesaHandler>.Instance);

        private static TipoDespesa NovoTipo(int id = 1, string nome = "Pedágio", bool ativo = true) =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Ativo = ativo };

        [Fact]
        public async Task Create_DevePersistirEscopadoNaEmpresaEMapearResposta()
        {
            _repository.AddAsync(Arg.Any<TipoDespesa>()).Returns(c => c.Arg<TipoDespesa>());

            var handler = CriarCreateHandler();
            var resposta = await handler.HandleAsync(
                new CreateTipoDespesaCommand(new CreateTipoDespesaRequest { Nome = "  Pedágio  " }));

            await _repository.Received(1).AddAsync(Arg.Is<TipoDespesa>(t => t.EmpresaId == 1));
            // O nome é gravado sem espaços nas pontas — senão " Pedágio" e "Pedágio"
            // conviveriam apesar do índice único.
            Assert.Equal("Pedágio", resposta.Nome);
            Assert.True(resposta.Ativo);
        }

        [Fact]
        public async Task Create_ComNomeJaUsadoNaEmpresa_DeveRecusar()
        {
            _repository.ExisteNomeAsync(1, "Pedágio", null).Returns(true);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreateTipoDespesaCommand(
                    new CreateTipoDespesaRequest { Nome = "Pedágio" })));
        }

        [Fact]
        public async Task Create_DeveRegistrarAuditoria()
        {
            _repository.AddAsync(Arg.Any<TipoDespesa>()).Returns(c => c.Arg<TipoDespesa>());

            var handler = CriarCreateHandler();
            await handler.HandleAsync(new CreateTipoDespesaCommand(
                new CreateTipoDespesaRequest { Nome = "Pedágio" }));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.TipoDespesa, AcoesAuditoria.Criou,
                Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>?>());
        }

        [Fact]
        public async Task Update_DeveEscoparNaEmpresaERegistrarODiff()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _repository.UpdateAsync(Arg.Any<TipoDespesa>()).Returns(c => c.Arg<TipoDespesa>());

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdateTipoDespesaCommand(1,
                new UpdateTipoDespesaRequest { Nome = "Pedágio urbano", Ativo = false }));

            await _repository.Received(1).GetByIdAsync(1, 1);
            Assert.NotNull(resposta);
            Assert.False(resposta.Ativo);

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.TipoDespesa, AcoesAuditoria.Atualizou, 1, Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>?>(a => a != null && a.Any(c => c.Campo == "Nome")));
        }

        [Fact]
        public async Task Update_TipoInexistente_DeveDevolverNulo()
        {
            _repository.GetByIdAsync(1, 1).Returns((TipoDespesa?)null);

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdateTipoDespesaCommand(1,
                new UpdateTipoDespesaRequest { Nome = "Pedágio" }));

            Assert.Null(resposta);
        }

        [Fact]
        public async Task Delete_ComTipoEmUso_DeveRecusarPedindoParaInativar()
        {
            // Apagar o tipo levaria junto o nome que identifica o histórico financeiro.
            _repository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _despesaRepository.ExisteComTipoAsync(1, 1).Returns(true);

            var handler = CriarDeleteHandler();

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new DeleteTipoDespesaCommand(1)));

            Assert.Contains("Inative-o", erro.Message);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<TipoDespesa>());
        }

        [Fact]
        public async Task Delete_ComTipoSemUso_DeveRemoverERegistrarAuditoria()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovoTipo());
            _despesaRepository.ExisteComTipoAsync(1, 1).Returns(false);

            var handler = CriarDeleteHandler();
            var removido = await handler.HandleAsync(new DeleteTipoDespesaCommand(1));

            Assert.True(removido);
            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.TipoDespesa, AcoesAuditoria.Excluiu, 1, Arg.Any<string>(),
                Arg.Any<IEnumerable<AlteracaoCampo>?>());
        }

        [Fact]
        public async Task GetAll_DeveEscoparNaEmpresaERepassarApenasAtivos()
        {
            _repository.GetAllAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns([NovoTipo()]);

            var handler = CriarGetAllHandler();
            await handler.HandleAsync(new GetAllTiposDespesaQuery(ApenasAtivos: true));

            await _repository.Received(1).GetAllAsync(1, true);
        }
    }
}
