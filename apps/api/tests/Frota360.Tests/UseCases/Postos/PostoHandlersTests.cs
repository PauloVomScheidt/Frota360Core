using Frota360.Application.Common;
using Frota360.Application.DTOs.Posto.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Postos.Commands.CreatePosto;
using Frota360.Application.UseCases.Postos.Commands.DeletePosto;
using Frota360.Application.UseCases.Postos.Commands.UpdatePosto;
using Frota360.Application.UseCases.Postos.Queries.GetAllPostos;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Postos
{
    public class PostoHandlersTests
    {
        private readonly IPostoRepository _repository = Substitute.For<IPostoRepository>();
        private readonly IAbastecimentoRepository _abastecimentoRepository = Substitute.For<IAbastecimentoRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly IAuditoriaService _auditoria = Substitute.For<IAuditoriaService>();

        public PostoHandlersTests() => _currentUser.EmpresaId.Returns(1);

        private CreatePostoHandler CriarCreateHandler() =>
            new(_repository, _currentUser, _auditoria, NullLogger<CreatePostoHandler>.Instance);

        private UpdatePostoHandler CriarUpdateHandler() =>
            new(_repository, _currentUser, _auditoria, NullLogger<UpdatePostoHandler>.Instance);

        private DeletePostoHandler CriarDeleteHandler() =>
            new(_repository, _abastecimentoRepository, _currentUser, _auditoria,
                NullLogger<DeletePostoHandler>.Instance);

        private GetAllPostosHandler CriarGetAllHandler() =>
            new(_repository, _currentUser, NullLogger<GetAllPostosHandler>.Instance);

        private static Posto NovoPosto(int id = 1, string nome = "Posto Ipiranga", bool ativo = true) =>
            new() { Id = id, EmpresaId = 1, Nome = nome, Ativo = ativo };

        [Fact]
        public async Task Create_DevePersistirEscopadoNaEmpresaEMapearResposta()
        {
            _repository.AddAsync(Arg.Any<Posto>()).Returns(c => c.Arg<Posto>());

            var handler = CriarCreateHandler();
            var resposta = await handler.HandleAsync(
                new CreatePostoCommand(new CreatePostoRequest { Nome = "  Posto Ipiranga  " }));

            await _repository.Received(1).AddAsync(Arg.Is<Posto>(t => t.EmpresaId == 1));
            // O nome é gravado sem espaços nas pontas — senão " Pedágio" e "Posto Ipiranga"
            // conviveriam apesar do índice único.
            Assert.Equal("Posto Ipiranga", resposta.Nome);
            Assert.True(resposta.Ativo);
        }

        [Fact]
        public async Task Create_ComNomeJaUsadoNaEmpresa_DeveRecusar()
        {
            _repository.ExisteNomeAsync(1, "Posto Ipiranga", null).Returns(true);

            var handler = CriarCreateHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new CreatePostoCommand(
                    new CreatePostoRequest { Nome = "Posto Ipiranga" })));
        }

        [Fact]
        public async Task Create_DeveRegistrarAuditoria()
        {
            _repository.AddAsync(Arg.Any<Posto>()).Returns(c => c.Arg<Posto>());

            var handler = CriarCreateHandler();
            await handler.HandleAsync(new CreatePostoCommand(
                new CreatePostoRequest { Nome = "Posto Ipiranga" }));

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Posto, AcoesAuditoria.Criou,
                Arg.Any<int?>(), Arg.Any<string>(), Arg.Any<IEnumerable<AlteracaoCampo>?>());
        }

        [Fact]
        public async Task Update_DeveEscoparNaEmpresaERegistrarODiff()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovoPosto());
            _repository.UpdateAsync(Arg.Any<Posto>()).Returns(c => c.Arg<Posto>());

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdatePostoCommand(1,
                new UpdatePostoRequest { Nome = "Posto Ipiranga BR-101", Ativo = false }));

            await _repository.Received(1).GetByIdAsync(1, 1);
            Assert.NotNull(resposta);
            Assert.False(resposta.Ativo);

            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Posto, AcoesAuditoria.Atualizou, 1, Arg.Any<string>(),
                Arg.Is<IEnumerable<AlteracaoCampo>?>(a => a != null && a.Any(c => c.Campo == "Nome")));
        }

        [Fact]
        public async Task Update_TipoInexistente_DeveDevolverNulo()
        {
            _repository.GetByIdAsync(1, 1).Returns((Posto?)null);

            var handler = CriarUpdateHandler();
            var resposta = await handler.HandleAsync(new UpdatePostoCommand(1,
                new UpdatePostoRequest { Nome = "Posto Ipiranga" }));

            Assert.Null(resposta);
        }

        [Fact]
        public async Task Delete_ComTipoEmUso_DeveRecusarPedindoParaInativar()
        {
            // Apagar o tipo levaria junto o nome que identifica o histórico de abastecimento.
            _repository.GetByIdAsync(1, 1).Returns(NovoPosto());
            _abastecimentoRepository.ExisteComPostoAsync(1, 1).Returns(true);

            var handler = CriarDeleteHandler();

            var erro = await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(new DeletePostoCommand(1)));

            Assert.Contains("Inative-o", erro.Message);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Posto>());
        }

        [Fact]
        public async Task Delete_ComTipoSemUso_DeveRemoverERegistrarAuditoria()
        {
            _repository.GetByIdAsync(1, 1).Returns(NovoPosto());
            _abastecimentoRepository.ExisteComPostoAsync(1, 1).Returns(false);

            var handler = CriarDeleteHandler();
            var removido = await handler.HandleAsync(new DeletePostoCommand(1));

            Assert.True(removido);
            await _auditoria.Received(1).RegistrarAsync(
                EntidadesAuditadas.Posto, AcoesAuditoria.Excluiu, 1, Arg.Any<string>(),
                Arg.Any<IEnumerable<AlteracaoCampo>?>());
        }

        [Fact]
        public async Task GetAll_DeveEscoparNaEmpresaERepassarApenasAtivos()
        {
            _repository.GetAllAsync(Arg.Any<int>(), Arg.Any<bool>()).Returns([NovoPosto()]);

            var handler = CriarGetAllHandler();
            await handler.HandleAsync(new GetAllPostosQuery(ApenasAtivos: true));

            await _repository.Received(1).GetAllAsync(1, true);
        }
    }
}
