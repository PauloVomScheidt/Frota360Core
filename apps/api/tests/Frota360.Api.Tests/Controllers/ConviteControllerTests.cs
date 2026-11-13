using Frota360.Api.Controllers;
using Frota360.Application.DTOs.Convite.Request;
using Frota360.Application.DTOs.Convite.Response;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class ConviteControllerTests
    {
        private readonly IConviteService _conviteService = Substitute.For<IConviteService>();
        private readonly DefaultHttpContext _http = new();

        private ConviteController Controller(bool valido = true)
        {
            var controller = valido
                ? new ConviteController(_conviteService, Validacao.Aceita<CriarConviteRequest>(),
                    Validacao.Aceita<AceitarConviteRequest>(), Validacao.Aceita<ConsultarConviteRequest>())
                : new ConviteController(_conviteService, Validacao.Recusa<CriarConviteRequest>(),
                    Validacao.Recusa<AceitarConviteRequest>(), Validacao.Recusa<ConsultarConviteRequest>());

            controller.ControllerContext = new ControllerContext { HttpContext = _http };
            return controller;
        }

        [Fact]
        public async Task Listar_DeveDevolverOsConvitesDaEmpresa()
        {
            IEnumerable<ConviteResponse> convites = [new() { Id = 1 }];
            _conviteService.ListarAsync().Returns(convites);

            Assert.Same(convites, Resposta.Ok<IEnumerable<ConviteResponse>>(await Controller().Listar()));
        }

        [Fact]
        public async Task Criar_DeveResponder201ComOConvite()
        {
            var request = new CriarConviteRequest();
            var convite = new ConviteCriadoResponse { Id = 5 };
            _conviteService.CriarAsync(request).Returns(convite);

            var criado = Assert.IsType<CreatedResult>(await Controller().Criar(request));
            var envelope = Assert.IsType<ApiResponse<ConviteCriadoResponse>>(criado.Value);
            Assert.Same(convite, envelope.Dados);
            Assert.Equal("Convite enviado com sucesso.", envelope.Mensagem);
        }

        [Fact]
        public async Task Cancelar_DeveConfirmarOu404()
        {
            _conviteService.CancelarAsync(5).Returns(true);

            Resposta.OkSemDados(await Controller().Cancelar(5), "Convite cancelado com sucesso.");
            Resposta.NaoEncontrado(await Controller().Cancelar(6), "Convite 6 não encontrado.");
        }

        [Fact]
        public async Task Consultar_DeveDevolverOConviteOu404()
        {
            var valido = new ConsultarConviteRequest { Token = "bom" };
            var convite = new ConviteConsultadoResponse();
            _conviteService.ConsultarAsync(valido).Returns(convite);

            Assert.Same(convite, Resposta.Ok<ConviteConsultadoResponse>(await Controller().Consultar(valido)));
            Resposta.NaoEncontrado(await Controller().Consultar(new ConsultarConviteRequest { Token = "ruim" }),
                "Este convite é inválido, já foi utilizado ou expirou.");
        }

        [Fact]
        public async Task Aceitar_DeveCriarAContaEJaDeixarASessaoNosCookies()
        {
            var request = new AceitarConviteRequest { Token = "bom" };
            _conviteService.AceitarAsync(request).Returns(new AuthResponse { Token = "jwt", RefreshToken = "refresh", Nome = "Maria" });

            var sessao = Resposta.Ok<SessaoResponse>(await Controller().Aceitar(request), "Conta criada com sucesso.");

            Assert.Equal("Maria", sessao.Nome);
            Assert.Contains(_http.Response.Headers.SetCookie, c => c!.StartsWith($"{CookiesDeSessao.Token}=jwt;"));
        }

        [Fact]
        public async Task Aceitar_ComConviteExpirado_DeveResponder400SemCookie()
        {
            Resposta.Falha(await Controller().Aceitar(new AceitarConviteRequest()), StatusCodes.Status400BadRequest, "Convite inválido ou expirado.");
            Assert.Equal(0, _http.Response.Headers.SetCookie.Count);
        }

        [Fact]
        public async Task Formularios_ComDadosInvalidos_DevemResponder400SemChamarOServico()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Criar(new CriarConviteRequest()));
            Resposta.Invalido(await controller.Consultar(new ConsultarConviteRequest()));
            Resposta.Invalido(await controller.Aceitar(new AceitarConviteRequest()));
            Assert.Empty(_conviteService.ReceivedCalls());
        }
    }
}
