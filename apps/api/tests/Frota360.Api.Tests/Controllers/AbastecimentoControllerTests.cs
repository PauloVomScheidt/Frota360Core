using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.UseCases.Abastecimentos.Commands.CreateAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Commands.DeleteAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Commands.UpdateAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoAnterior;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoById;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetResumoAbastecimentos;
using Frota360.Domain.Common;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class AbastecimentoControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private AbastecimentoController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateAbastecimentoRequest>(), Validacao.Aceita<UpdateAbastecimentoRequest>(), Validacao.Aceita<ConsultarAbastecimentosRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateAbastecimentoRequest>(), Validacao.Recusa<UpdateAbastecimentoRequest>(), Validacao.Recusa<ConsultarAbastecimentosRequest>());

        [Fact]
        public async Task GetAll_DeveDevolverAPaginaDoFiltro()
        {
            var filtro = new ConsultarAbastecimentosRequest();
            var pagina = new ResultadoPaginado<AbastecimentoResponse> { Total = 12 };
            _dispatcher.SendAsync(Arg.Is<GetAllAbastecimentosQuery>(q => q.Filtro == filtro)).Returns(pagina);

            Assert.Same(pagina, Resposta.Ok<ResultadoPaginado<AbastecimentoResponse>>(await Controller().GetAll(filtro)));
        }

        [Fact]
        public async Task GetResumo_DeveDevolverOResumoDoFiltroInteiro()
        {
            var filtro = new ConsultarAbastecimentosRequest();
            var resumo = new ResumoAbastecimentosResponse();
            _dispatcher.SendAsync(Arg.Is<GetResumoAbastecimentosQuery>(q => q.Filtro == filtro)).Returns(resumo);

            Assert.Same(resumo, Resposta.Ok<ResumoAbastecimentosResponse>(await Controller().GetResumo(filtro)));
        }

        [Fact]
        public async Task Consultas_ComFiltroInvalido_DevemResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.GetAll(new ConsultarAbastecimentosRequest()));
            Resposta.Invalido(await controller.GetResumo(new ConsultarAbastecimentosRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task GetAnterior_DeveRepassarVeiculoOdometroEOLancamentoIgnorado()
        {
            var anterior = new AbastecimentoAnteriorResponse();
            _dispatcher.SendAsync(Arg.Is<GetAbastecimentoAnteriorQuery>(q => q.VeiculoId == 5 && q.Odometro == 120_000 && q.IgnorarId == 9))
                .Returns(anterior);

            Assert.Same(anterior, Resposta.Ok<AbastecimentoAnteriorResponse?>(await Controller().GetAnterior(5, 120_000, 9)));
        }

        [Fact]
        public async Task GetById_DeveDevolverOLancamentoOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetAbastecimentoByIdQuery>(q => q.Id == 7)).Returns(new AbastecimentoResponse { Id = 7 });

            Assert.Equal(7, Resposta.Ok<AbastecimentoResponse>(await Controller().GetById(7)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(8), "Abastecimento 8 não encontrado.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreateAbastecimentoRequest();
            _dispatcher.SendAsync(Arg.Is<CreateAbastecimentoCommand>(c => c.Data == request)).Returns(new AbastecimentoResponse { Id = 7 });

            Resposta.Criado<AbastecimentoResponse>(await Controller().Create(request), 7, "Abastecimento lançado com sucesso.");
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateAbastecimentoRequest()));
            Resposta.Invalido(await controller.Update(7, new UpdateAbastecimentoRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverACorrecaoOu404()
        {
            var request = new UpdateAbastecimentoRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateAbastecimentoCommand>(c => c.Id == 7 && c.Data == request)).Returns(new AbastecimentoResponse { Id = 7 });

            Resposta.Ok<AbastecimentoResponse>(await Controller().Update(7, request), "Abastecimento corrigido com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(8, request), "Abastecimento 8 não encontrado.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteAbastecimentoCommand>(c => c.Id == 7)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(7), "Abastecimento removido com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(8), "Abastecimento 8 não encontrado.");
        }
    }
}
