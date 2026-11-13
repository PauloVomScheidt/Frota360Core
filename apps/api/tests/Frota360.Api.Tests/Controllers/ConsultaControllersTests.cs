using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Auditoria.Request;
using Frota360.Application.DTOs.Auditoria.Response;
using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.DTOs.Custo.Response;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.UseCases.Auditoria.Queries.GetLogsAuditoria;
using Frota360.Application.UseCases.Custos.Queries.GetCustos;
using Frota360.Application.UseCases.Custos.Queries.GetResumoCustos;
using Frota360.Application.UseCases.Motoristas.Queries.GetAllMotoristas;
using Frota360.Application.UseCases.Motoristas.Queries.GetMotoristaById;
using Frota360.Domain.Common;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    /// <summary>Os três controllers só de leitura: custos, auditoria e motoristas.</summary>
    public class ConsultaControllersTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        [Fact]
        public async Task Custo_Consultar_DeveDevolverAPaginaDoFiltro()
        {
            var filtro = new ConsultarCustosRequest();
            var pagina = new ResultadoPaginado<LancamentoCustoResponse> { Total = 5 };
            _dispatcher.SendAsync(Arg.Is<GetCustosQuery>(q => q.Data == filtro)).Returns(pagina);
            var controller = new CustoController(_dispatcher, Validacao.Aceita<ConsultarCustosRequest>(), Validacao.Aceita<ResumoCustosRequest>());

            Assert.Same(pagina, Resposta.Ok<ResultadoPaginado<LancamentoCustoResponse>>(await controller.Consultar(filtro)));
        }

        [Fact]
        public async Task Custo_Resumir_DeveDevolverOsKpisDoPeriodo()
        {
            var filtro = new ResumoCustosRequest();
            var resumo = new ResumoCustosResponse();
            _dispatcher.SendAsync(Arg.Is<GetResumoCustosQuery>(q => q.Data == filtro)).Returns(resumo);
            var controller = new CustoController(_dispatcher, Validacao.Aceita<ConsultarCustosRequest>(), Validacao.Aceita<ResumoCustosRequest>());

            Assert.Same(resumo, Resposta.Ok<ResumoCustosResponse>(await controller.Resumir(filtro)));
        }

        [Fact]
        public async Task Custo_ComFiltroInvalido_DeveResponder400SemDespachar()
        {
            var controller = new CustoController(_dispatcher, Validacao.Recusa<ConsultarCustosRequest>(), Validacao.Recusa<ResumoCustosRequest>());

            Resposta.Invalido(await controller.Consultar(new ConsultarCustosRequest()), "Filtro inválido.");
            Resposta.Invalido(await controller.Resumir(new ResumoCustosRequest()), "Filtro inválido.");
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Auditoria_Consultar_DeveDevolverAPaginaOu400()
        {
            var filtro = new ConsultarAuditoriaRequest();
            var pagina = new ResultadoPaginado<LogAuditoriaResponse> { Total = 50 };
            _dispatcher.SendAsync(Arg.Is<GetLogsAuditoriaQuery>(q => q.Filtro == filtro)).Returns(pagina);

            var valido = new AuditoriaController(_dispatcher, Validacao.Aceita<ConsultarAuditoriaRequest>());
            var invalido = new AuditoriaController(_dispatcher, Validacao.Recusa<ConsultarAuditoriaRequest>());

            Assert.Same(pagina, Resposta.Ok<ResultadoPaginado<LogAuditoriaResponse>>(await valido.Consultar(filtro)));
            Resposta.Invalido(await invalido.Consultar(filtro), "Filtro inválido.");
        }

        [Fact]
        public async Task Motorista_DeveListarEBuscarPorIdOu404()
        {
            IEnumerable<MotoristaResponse> motoristas = [new() { Id = 3 }];
            _dispatcher.SendAsync(Arg.Any<GetAllMotoristasQuery>()).Returns(motoristas);
            _dispatcher.SendAsync(Arg.Is<GetMotoristaByIdQuery>(q => q.Id == 3)).Returns(new MotoristaResponse { Id = 3 });
            var controller = new MotoristaController(_dispatcher);

            Assert.Same(motoristas, Resposta.Ok<IEnumerable<MotoristaResponse>>(await controller.GetAll()));
            Assert.Equal(3, Resposta.Ok<MotoristaResponse>(await controller.GetById(3)).Id);
            Resposta.NaoEncontrado(await controller.GetById(4), "Motorista 4 não encontrado.");
        }
    }
}
