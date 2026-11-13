using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Request;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.UseCases.Manutencoes.Commands.ConcluirManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.CreateManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.DeleteManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.UpdateManutencao;
using Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes;
using Frota360.Application.UseCases.Manutencoes.Queries.GetManutencaoById;
using Frota360.Application.UseCases.Manutencoes.Queries.GetResumoManutencoes;
using Frota360.Domain.Common;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class ManutencaoControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private ManutencaoController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateManutencaoRequest>(), Validacao.Aceita<UpdateManutencaoRequest>(),
                Validacao.Aceita<ConcluirManutencaoRequest>(), Validacao.Aceita<ConsultarManutencoesRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateManutencaoRequest>(), Validacao.Recusa<UpdateManutencaoRequest>(),
                Validacao.Recusa<ConcluirManutencaoRequest>(), Validacao.Recusa<ConsultarManutencoesRequest>());

        [Fact]
        public async Task GetAll_DeveDevolverAPaginaDoFiltroOu400()
        {
            var filtro = new ConsultarManutencoesRequest();
            var pagina = new ResultadoPaginado<ManutencaoResponse> { Total = 4 };
            _dispatcher.SendAsync(Arg.Is<GetAllManutencoesQuery>(q => q.Filtro == filtro)).Returns(pagina);

            Assert.Same(pagina, Resposta.Ok<ResultadoPaginado<ManutencaoResponse>>(await Controller().GetAll(filtro)));
            Resposta.Invalido(await Controller(valido: false).GetAll(filtro));
        }

        [Fact]
        public async Task GetResumo_DeveDevolverOsContadoresDoDashboard()
        {
            var resumo = new ResumoManutencoesResponse();
            _dispatcher.SendAsync(Arg.Any<GetResumoManutencoesQuery>()).Returns(resumo);

            Assert.Same(resumo, Resposta.Ok<ResumoManutencoesResponse>(await Controller().GetResumo()));
        }

        [Fact]
        public async Task GetById_DeveDevolverAManutencaoOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetManutencaoByIdQuery>(q => q.Id == 7)).Returns(new ManutencaoResponse { Id = 7 });

            Assert.Equal(7, Resposta.Ok<ManutencaoResponse>(await Controller().GetById(7)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(8), "Manutenção 8 não encontrada.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreateManutencaoRequest();
            _dispatcher.SendAsync(Arg.Is<CreateManutencaoCommand>(c => c.Data == request)).Returns(new ManutencaoResponse { Id = 7 });

            Resposta.Criado<ManutencaoResponse>(await Controller().Create(request), 7, "Manutenção agendada com sucesso.");
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateManutencaoRequest()));
            Resposta.Invalido(await controller.Update(7, new UpdateManutencaoRequest()));
            Resposta.Invalido(await controller.Concluir(7, new ConcluirManutencaoRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverAManutencaoOu404()
        {
            var request = new UpdateManutencaoRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateManutencaoCommand>(c => c.Id == 7 && c.Data == request)).Returns(new ManutencaoResponse { Id = 7 });

            Resposta.Ok<ManutencaoResponse>(await Controller().Update(7, request), "Manutenção atualizada com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(8, request), "Manutenção 8 não encontrada.");
        }

        [Fact]
        public async Task Concluir_DeveDevolverAManutencaoConcluidaOu404()
        {
            var request = new ConcluirManutencaoRequest();
            _dispatcher.SendAsync(Arg.Is<ConcluirManutencaoCommand>(c => c.Id == 7 && c.Data == request)).Returns(new ManutencaoResponse { Id = 7 });

            Resposta.Ok<ManutencaoResponse>(await Controller().Concluir(7, request), "Manutenção concluída com sucesso.");
            Resposta.NaoEncontrado(await Controller().Concluir(8, request), "Manutenção 8 não encontrada.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteManutencaoCommand>(c => c.Id == 7)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(7), "Manutenção removida com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(8), "Manutenção 8 não encontrada.");
        }
    }
}
