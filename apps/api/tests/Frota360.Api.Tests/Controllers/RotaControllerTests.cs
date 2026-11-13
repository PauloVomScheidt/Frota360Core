using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.UseCases.Rotas.Commands.CalcularRota;
using Frota360.Application.UseCases.Rotas.Commands.CreateRota;
using Frota360.Application.UseCases.Rotas.Commands.DeleteRota;
using Frota360.Application.UseCases.Rotas.Commands.EncerrarRota;
using Frota360.Application.UseCases.Rotas.Commands.UpdateRota;
using Frota360.Application.UseCases.Rotas.Queries.GetAllRotas;
using Frota360.Application.UseCases.Rotas.Queries.GetMinhasRotas;
using Frota360.Application.UseCases.Rotas.Queries.GetResumoRotas;
using Frota360.Application.UseCases.Rotas.Queries.GetRotaById;
using Frota360.Domain.Common;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class RotaControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private RotaController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateRotaRequest>(), Validacao.Aceita<UpdateRotaRequest>(),
                Validacao.Aceita<EncerrarRotaRequest>(), Validacao.Aceita<ConsultarRotasRequest>(), Validacao.Aceita<CalcularRotaRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateRotaRequest>(), Validacao.Recusa<UpdateRotaRequest>(),
                Validacao.Recusa<EncerrarRotaRequest>(), Validacao.Recusa<ConsultarRotasRequest>(), Validacao.Recusa<CalcularRotaRequest>());

        [Fact]
        public async Task GetAll_DeveDevolverAsRotasDaEmpresa()
        {
            var filtro = new ConsultarRotasRequest();
            var pagina = new ResultadoPaginado<RotaResponse> { Total = 2 };
            _dispatcher.SendAsync(Arg.Is<GetAllRotasQuery>(q => q.Filtro == filtro)).Returns(pagina);

            Assert.Same(pagina, Resposta.Ok<ResultadoPaginado<RotaResponse>>(await Controller().GetAll(filtro)));
        }

        [Fact]
        public async Task GetMinhas_DeveDevolverSoAsRotasDoMotorista()
        {
            var filtro = new ConsultarRotasRequest();
            var pagina = new ResultadoPaginado<RotaResponse> { Total = 1 };
            _dispatcher.SendAsync(Arg.Is<GetMinhasRotasQuery>(q => q.Filtro == filtro)).Returns(pagina);

            Assert.Same(pagina, Resposta.Ok<ResultadoPaginado<RotaResponse>>(await Controller().GetMinhas(filtro)));
        }

        [Fact]
        public async Task Consultas_ComFiltroInvalido_DevemResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.GetAll(new ConsultarRotasRequest()));
            Resposta.Invalido(await controller.GetMinhas(new ConsultarRotasRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task GetResumo_DeveRepassarOPeriodo()
        {
            var de = new DateTime(2026, 11, 1);
            var ate = new DateTime(2026, 11, 30);
            var resumo = new ResumoRotasResponse();
            _dispatcher.SendAsync(Arg.Is<GetResumoRotasQuery>(q => q.De == de && q.Ate == ate)).Returns(resumo);

            Assert.Same(resumo, Resposta.Ok<ResumoRotasResponse>(await Controller().GetResumo(de, ate)));
        }

        [Fact]
        public async Task GetById_DeveDevolverARotaOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetRotaByIdQuery>(q => q.Id == 7)).Returns(new RotaResponse { Id = 7 });

            Assert.Equal(7, Resposta.Ok<RotaResponse>(await Controller().GetById(7)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(8), "Rota 8 não encontrada.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreateRotaRequest();
            _dispatcher.SendAsync(Arg.Is<CreateRotaCommand>(c => c.Data == request)).Returns(new RotaResponse { Id = 7 });

            Resposta.Criado<RotaResponse>(await Controller().Create(request), 7, "Rota cadastrada com sucesso.");
        }

        [Fact]
        public async Task Calcular_DeveDevolverOTrajetoCalculado()
        {
            var request = new CalcularRotaRequest();
            var calculo = new CalculoRotaResponse();
            _dispatcher.SendAsync(Arg.Is<CalcularRotaCommand>(c => c.Data == request)).Returns(calculo);

            Assert.Same(calculo, Resposta.Ok<CalculoRotaResponse>(await Controller().Calcular(request), "Trajeto calculado com sucesso."));
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateRotaRequest()));
            Resposta.Invalido(await controller.Calcular(new CalcularRotaRequest()));
            Resposta.Invalido(await controller.Update(7, new UpdateRotaRequest()));
            Resposta.Invalido(await controller.Encerrar(7, new EncerrarRotaRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverARotaOu404()
        {
            var request = new UpdateRotaRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateRotaCommand>(c => c.Id == 7 && c.Data == request)).Returns(new RotaResponse { Id = 7 });

            Resposta.Ok<RotaResponse>(await Controller().Update(7, request), "Rota atualizada com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(8, request), "Rota 8 não encontrada.");
        }

        [Fact]
        public async Task Encerrar_DeveDevolverARotaEncerradaOu404()
        {
            var request = new EncerrarRotaRequest();
            _dispatcher.SendAsync(Arg.Is<EncerrarRotaCommand>(c => c.Id == 7 && c.Data == request)).Returns(new RotaResponse { Id = 7 });

            Resposta.Ok<RotaResponse>(await Controller().Encerrar(7, request), "Rota encerrada com sucesso.");
            Resposta.NaoEncontrado(await Controller().Encerrar(8, request), "Rota 8 não encontrada.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteRotaCommand>(c => c.Id == 7)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(7), "Rota removida com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(8), "Rota 8 não encontrada.");
        }
    }
}
