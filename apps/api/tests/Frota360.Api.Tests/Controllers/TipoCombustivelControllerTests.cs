using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Request;
using Frota360.Application.DTOs.TipoCombustivel.Response;
using Frota360.Application.UseCases.TiposCombustivel.Commands.CreateTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Commands.DeleteTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Commands.UpdateTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Queries.GetAllTiposCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Queries.GetTipoCombustivelById;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class TipoCombustivelControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private TipoCombustivelController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateTipoCombustivelRequest>(), Validacao.Aceita<UpdateTipoCombustivelRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateTipoCombustivelRequest>(), Validacao.Recusa<UpdateTipoCombustivelRequest>());

        [Fact]
        public async Task GetAll_DeveRepassarOFiltroDeAtivos()
        {
            IEnumerable<TipoCombustivelResponse> tipos = [new() { Id = 1 }];
            _dispatcher.SendAsync(Arg.Is<GetAllTiposCombustivelQuery>(q => !q.ApenasAtivos)).Returns(tipos);

            Assert.Same(tipos, Resposta.Ok<IEnumerable<TipoCombustivelResponse>>(await Controller().GetAll()));
        }

        [Fact]
        public async Task GetById_DeveDevolverOTipoOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetTipoCombustivelByIdQuery>(q => q.Id == 3)).Returns(new TipoCombustivelResponse { Id = 3 });

            Assert.Equal(3, Resposta.Ok<TipoCombustivelResponse>(await Controller().GetById(3)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(4), "Tipo de combustível 4 não encontrado.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreateTipoCombustivelRequest();
            _dispatcher.SendAsync(Arg.Is<CreateTipoCombustivelCommand>(c => c.Data == request)).Returns(new TipoCombustivelResponse { Id = 3 });

            Resposta.Criado<TipoCombustivelResponse>(await Controller().Create(request), 3, "Tipo de combustível cadastrado com sucesso.");
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateTipoCombustivelRequest()));
            Resposta.Invalido(await controller.Update(3, new UpdateTipoCombustivelRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverOTipoOu404()
        {
            var request = new UpdateTipoCombustivelRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateTipoCombustivelCommand>(c => c.Id == 3 && c.Data == request)).Returns(new TipoCombustivelResponse { Id = 3 });

            Resposta.Ok<TipoCombustivelResponse>(await Controller().Update(3, request), "Tipo de combustível atualizado com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(4, request), "Tipo de combustível 4 não encontrado.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteTipoCombustivelCommand>(c => c.Id == 3)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(3), "Tipo de combustível removido com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(4), "Tipo de combustível 4 não encontrado.");
        }
    }
}
