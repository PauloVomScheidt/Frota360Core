using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Request;
using Frota360.Application.DTOs.TipoDespesa.Response;
using Frota360.Application.UseCases.TiposDespesa.Commands.CreateTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Commands.DeleteTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Commands.UpdateTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Queries.GetAllTiposDespesa;
using Frota360.Application.UseCases.TiposDespesa.Queries.GetTipoDespesaById;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class TipoDespesaControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private TipoDespesaController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateTipoDespesaRequest>(), Validacao.Aceita<UpdateTipoDespesaRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateTipoDespesaRequest>(), Validacao.Recusa<UpdateTipoDespesaRequest>());

        [Fact]
        public async Task GetAll_DeveRepassarOFiltroDeAtivos()
        {
            IEnumerable<TipoDespesaResponse> tipos = [new() { Id = 1 }];
            _dispatcher.SendAsync(Arg.Is<GetAllTiposDespesaQuery>(q => q.ApenasAtivos)).Returns(tipos);

            Assert.Same(tipos, Resposta.Ok<IEnumerable<TipoDespesaResponse>>(await Controller().GetAll(apenasAtivos: true)));
        }

        [Fact]
        public async Task GetById_DeveDevolverOTipoOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetTipoDespesaByIdQuery>(q => q.Id == 3)).Returns(new TipoDespesaResponse { Id = 3 });

            Assert.Equal(3, Resposta.Ok<TipoDespesaResponse>(await Controller().GetById(3)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(4), "Tipo de despesa 4 não encontrado.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreateTipoDespesaRequest();
            _dispatcher.SendAsync(Arg.Is<CreateTipoDespesaCommand>(c => c.Data == request)).Returns(new TipoDespesaResponse { Id = 3 });

            Resposta.Criado<TipoDespesaResponse>(await Controller().Create(request), 3, "Tipo de despesa cadastrado com sucesso.");
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateTipoDespesaRequest()));
            Resposta.Invalido(await controller.Update(3, new UpdateTipoDespesaRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverOTipoOu404()
        {
            var request = new UpdateTipoDespesaRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateTipoDespesaCommand>(c => c.Id == 3 && c.Data == request)).Returns(new TipoDespesaResponse { Id = 3 });

            Resposta.Ok<TipoDespesaResponse>(await Controller().Update(3, request), "Tipo de despesa atualizado com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(4, request), "Tipo de despesa 4 não encontrado.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteTipoDespesaCommand>(c => c.Id == 3)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(3), "Tipo de despesa removido com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(4), "Tipo de despesa 4 não encontrado.");
        }
    }
}
