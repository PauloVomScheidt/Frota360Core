using System.Security.Claims;
using Frota360.Api.Controllers;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly IAuthService _authService = Substitute.For<IAuthService>();
        private readonly DefaultHttpContext _http = new();

        private AuthController Controller(bool valido = true)
        {
            var controller = valido
                ? new AuthController(_authService, Validacao.Aceita<LoginRequest>(), Validacao.Aceita<RefreshTokenRequest>(),
                    Validacao.Aceita<EsqueciSenhaRequest>(), Validacao.Aceita<RedefinirSenhaRequest>())
                : new AuthController(_authService, Validacao.Recusa<LoginRequest>(), Validacao.Recusa<RefreshTokenRequest>(),
                    Validacao.Recusa<EsqueciSenhaRequest>(), Validacao.Recusa<RedefinirSenhaRequest>());

            controller.ControllerContext = new ControllerContext { HttpContext = _http };
            return controller;
        }

        private static AuthResponse Sessao() => new()
        {
            Token = "jwt",
            RefreshToken = "refresh",
            Nome = "Maria",
            Email = "maria@exemplo.com",
            Role = Roles.Admin,
            NomeEmpresa = "Transportes Exemplo"
        };

        private string[] CookiesEmitidos() => _http.Response.Headers.SetCookie.Select(c => c!).ToArray();

        [Fact]
        public async Task Login_ComCredenciais_DeveEmitirOsCookiesSemPorOTokenNoCorpo()
        {
            var request = new LoginRequest { Email = "maria@exemplo.com", Senha = "SenhaForte123" };
            _authService.LoginAsync(request).Returns(Sessao());

            var sessao = Resposta.Ok<SessaoResponse>(await Controller().Login(request), "Login realizado com sucesso.");

            Assert.Equal("Maria", sessao.Nome);
            Assert.Equal("Transportes Exemplo", sessao.NomeEmpresa);
            Assert.Contains(CookiesEmitidos(), c => c.StartsWith($"{CookiesDeSessao.Token}=jwt;"));
            Assert.Contains(CookiesEmitidos(), c => c.StartsWith($"{CookiesDeSessao.Refresh}=refresh;"));
        }

        [Fact]
        public async Task Login_ComCredenciaisErradas_DeveResponder401SemCookie()
        {
            Resposta.Falha(await Controller().Login(new LoginRequest()), StatusCodes.Status401Unauthorized, "E-mail ou senha inválidos.");
            Assert.Empty(CookiesEmitidos());
        }

        [Fact]
        public async Task Refresh_SemCookie_DeveResponder401SemConsultarOServico()
        {
            Resposta.Falha(await Controller().Refresh(), StatusCodes.Status401Unauthorized, "Refresh token inválido ou expirado.");
            await _authService.DidNotReceiveWithAnyArgs().RefreshAsync(default!);
        }

        [Fact]
        public async Task Refresh_ComCookieValido_DeveRenovarOsCookies()
        {
            _http.Request.Headers.Cookie = $"{CookiesDeSessao.Refresh}=refresh-antigo";
            _authService.RefreshAsync(Arg.Is<RefreshTokenRequest>(r => r.RefreshToken == "refresh-antigo")).Returns(Sessao());

            Resposta.Ok<SessaoResponse>(await Controller().Refresh(), "Token renovado com sucesso.");
            Assert.Contains(CookiesEmitidos(), c => c.StartsWith($"{CookiesDeSessao.Refresh}=refresh;"));
        }

        [Fact]
        public async Task Refresh_ComCookieExpirado_DeveResponder401()
        {
            _http.Request.Headers.Cookie = $"{CookiesDeSessao.Refresh}=expirado";

            Resposta.Falha(await Controller().Refresh(), StatusCodes.Status401Unauthorized, "Refresh token inválido ou expirado.");
        }

        [Fact]
        public async Task Refresh_ComCookieMalformado_DeveResponder400()
        {
            _http.Request.Headers.Cookie = $"{CookiesDeSessao.Refresh}=x";

            Resposta.Invalido(await Controller(valido: false).Refresh());
        }

        [Fact]
        public async Task EsqueciSenha_DeveResponderOMesmoTextoExistindoOuNaoOEmail()
        {
            var request = new EsqueciSenhaRequest { Email = "qualquer@exemplo.com" };

            Resposta.OkSemDados(await Controller().EsqueciSenha(request),
                "Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha.");
            await _authService.Received(1).EsqueciSenhaAsync(request);
        }

        [Fact]
        public async Task RedefinirSenha_DeveConfirmarOuRecusarOToken()
        {
            var valido = new RedefinirSenhaRequest { Token = "bom" };
            _authService.RedefinirSenhaAsync(valido).Returns(true);

            Resposta.OkSemDados(await Controller().RedefinirSenha(valido), "Senha redefinida com sucesso. Faça login com a nova senha.");
            Resposta.Falha(await Controller().RedefinirSenha(new RedefinirSenhaRequest { Token = "ruim" }),
                StatusCodes.Status400BadRequest, "Token de redefinição inválido ou expirado.");
        }

        [Fact]
        public async Task Formularios_ComDadosInvalidos_DevemResponder400SemChamarOServico()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Login(new LoginRequest()));
            Resposta.Invalido(await controller.EsqueciSenha(new EsqueciSenhaRequest()));
            Resposta.Invalido(await controller.RedefinirSenha(new RedefinirSenhaRequest()));
            Assert.Empty(_authService.ReceivedCalls());
        }

        [Fact]
        public async Task Logout_DeveRevogarORefreshDoUsuarioELimparOsCookies()
        {
            _http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "42")], "Bearer"));

            Resposta.OkSemDados(await Controller().Logout(), "Logout realizado com sucesso.");
            await _authService.Received(1).LogoutAsync(42);
            Assert.Equal(2, CookiesEmitidos().Length);
            Assert.All(CookiesEmitidos(), c => Assert.Contains("expires=Thu, 01 Jan 1970", c));
        }

        [Fact]
        public async Task Logout_SemIdentificacaoDoUsuario_DeveResponder401()
        {
            Resposta.Falha(await Controller().Logout(), StatusCodes.Status401Unauthorized, "Não autorizado.");
            await _authService.DidNotReceiveWithAnyArgs().LogoutAsync(default);
        }
    }
}
