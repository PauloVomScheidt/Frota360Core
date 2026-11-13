using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.UseCases.Despesas.Commands.CreateDespesa;
using Frota360.Application.UseCases.Despesas.Commands.DeleteDespesa;
using Frota360.Application.UseCases.Despesas.Commands.UpdateDespesa;
using Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas;
using Frota360.Application.UseCases.Despesas.Queries.GetDespesaById;
using Frota360.Application.UseCases.Despesas.Queries.GetResumoDespesas;
using Frota360.Domain.Common;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class DespesaControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private DespesaController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateDespesaRequest>(), Validacao.Aceita<UpdateDespesaRequest>(), Validacao.Aceita<ConsultarDespesasRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateDespesaRequest>(), Validacao.Recusa<UpdateDespesaRequest>(), Validacao.Recusa<ConsultarDespesasRequest>());

        [Fact]
        public async Task GetAll_DeveDevolverAPaginaDoFiltro()
        {
            var filtro = new ConsultarDespesasRequest { Pagina = 2 };
            var pagina = new ResultadoPaginado<DespesaResponse> { Pagina = 2, Total = 30 };
            _dispatcher.SendAsync(Arg.Is<GetAllDespesasQuery>(q => q.Filtro == filtro)).Returns(pagina);

            Assert.Same(pagina, Resposta.Ok<ResultadoPaginado<DespesaResponse>>(await Controller().GetAll(filtro)));
        }

        [Fact]
        public async Task GetResumo_DeveDevolverOResumoDoFiltroInteiro()
        {
            var filtro = new ConsultarDespesasRequest();
            var resumo = new ResumoDespesasResponse { Quantidade = 3, ValorTotal = 90m };
            _dispatcher.SendAsync(Arg.Is<GetResumoDespesasQuery>(q => q.Filtro == filtro)).Returns(resumo);

            Assert.Same(resumo, Resposta.Ok<ResumoDespesasResponse>(await Controller().GetResumo(filtro)));
        }

        [Fact]
        public async Task Consultas_ComFiltroInvalido_DevemResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.GetAll(new ConsultarDespesasRequest()));
            Resposta.Invalido(await controller.GetResumo(new ConsultarDespesasRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task GetById_DeveDevolverADespesaOu404()
        {
            var despesa = new DespesaResponse { Id = 7 };
            _dispatcher.SendAsync(Arg.Is<GetDespesaByIdQuery>(q => q.Id == 7)).Returns(despesa);

            Assert.Same(despesa, Resposta.Ok<DespesaResponse>(await Controller().GetById(7)));
            Resposta.NaoEncontrado(await Controller().GetById(8), "Despesa 8 não encontrada.");
        }

        [Fact]
        public async Task Create_DeveResponder201ApontandoParaOGetById()
        {
            var request = new CreateDespesaRequest();
            _dispatcher.SendAsync(Arg.Is<CreateDespesaCommand>(c => c.Data == request)).Returns(new DespesaResponse { Id = 7 });

            var criada = Resposta.Criado<DespesaResponse>(await Controller().Create(request), 7, "Despesa lançada com sucesso.");

            Assert.Equal(7, criada.Id);
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateDespesaRequest()));
            Resposta.Invalido(await controller.Update(7, new UpdateDespesaRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverACorrecaoOu404()
        {
            var request = new UpdateDespesaRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateDespesaCommand>(c => c.Id == 7 && c.Data == request)).Returns(new DespesaResponse { Id = 7 });

            Resposta.Ok<DespesaResponse>(await Controller().Update(7, request), "Despesa atualizada com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(8, request), "Despesa 8 não encontrada.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteDespesaCommand>(c => c.Id == 7)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(7), "Despesa removida com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(8), "Despesa 8 não encontrada.");
        }
    }
}
