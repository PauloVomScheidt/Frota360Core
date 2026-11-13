using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Request;
using Frota360.Application.DTOs.TipoManutencao.Response;
using Frota360.Application.UseCases.TiposManutencao.Commands.CreateTipoManutencao;
using Frota360.Application.UseCases.TiposManutencao.Commands.DeleteTipoManutencao;
using Frota360.Application.UseCases.TiposManutencao.Commands.UpdateTipoManutencao;
using Frota360.Application.UseCases.TiposManutencao.Queries.GetAllTiposManutencao;
using Frota360.Application.UseCases.TiposManutencao.Queries.GetTipoManutencaoById;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class TipoManutencaoControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private TipoManutencaoController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateTipoManutencaoRequest>(), Validacao.Aceita<UpdateTipoManutencaoRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateTipoManutencaoRequest>(), Validacao.Recusa<UpdateTipoManutencaoRequest>());

        [Fact]
        public async Task GetAll_DeveRepassarOFiltroDeAtivos()
        {
            IEnumerable<TipoManutencaoResponse> tipos = [new() { Id = 1 }];
            _dispatcher.SendAsync(Arg.Is<GetAllTiposManutencaoQuery>(q => q.ApenasAtivos)).Returns(tipos);

            Assert.Same(tipos, Resposta.Ok<IEnumerable<TipoManutencaoResponse>>(await Controller().GetAll(apenasAtivos: true)));
        }

        [Fact]
        public async Task GetById_DeveDevolverOTipoOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetTipoManutencaoByIdQuery>(q => q.Id == 3)).Returns(new TipoManutencaoResponse { Id = 3 });

            Assert.Equal(3, Resposta.Ok<TipoManutencaoResponse>(await Controller().GetById(3)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(4), "Tipo de manutenção 4 não encontrado.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreateTipoManutencaoRequest();
            _dispatcher.SendAsync(Arg.Is<CreateTipoManutencaoCommand>(c => c.Data == request)).Returns(new TipoManutencaoResponse { Id = 3 });

            Resposta.Criado<TipoManutencaoResponse>(await Controller().Create(request), 3, "Tipo de manutenção cadastrado com sucesso.");
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateTipoManutencaoRequest()));
            Resposta.Invalido(await controller.Update(3, new UpdateTipoManutencaoRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverOTipoOu404()
        {
            var request = new UpdateTipoManutencaoRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateTipoManutencaoCommand>(c => c.Id == 3 && c.Data == request)).Returns(new TipoManutencaoResponse { Id = 3 });

            Resposta.Ok<TipoManutencaoResponse>(await Controller().Update(3, request), "Tipo de manutenção atualizado com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(4, request), "Tipo de manutenção 4 não encontrado.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteTipoManutencaoCommand>(c => c.Id == 3)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(3), "Tipo de manutenção removido com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(4), "Tipo de manutenção 4 não encontrado.");
        }
    }
}
