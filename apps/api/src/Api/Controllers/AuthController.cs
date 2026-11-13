using Asp.Versioning;
using FluentValidation;
using Frota360.Api.Services;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Frota360.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController(IAuthService authService,
                            IValidator<LoginRequest> loginValidator,
                            IValidator<RefreshTokenRequest> refreshValidator,
                            IValidator<EsqueciSenhaRequest> esqueciSenhaValidator,
                            IValidator<RedefinirSenhaRequest> redefinirSenhaValidator) : ControllerBase
    {
        /// <summary>Autentica um usuário. Token JWT e refresh token saem em cookie HttpOnly, não no corpo.</summary>
        /// <response code="200">Login realizado com sucesso</response>
        /// <response code="401">Credenciais inválidas</response>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<SessaoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validation = await loginValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var response = await authService.LoginAsync(request);

            if (response is null)
                return Unauthorized(ApiResponse<object>.Fail("E-mail ou senha inválidos."));

            SessaoCookies.Emitir(Response, response.Token, response.RefreshToken);
            return Ok(ApiResponse<SessaoResponse>.Ok(response.ToSessaoResponse(), "Login realizado com sucesso."));
        }

        /// <summary>Renova o token JWT a partir do refresh token do cookie (rotaciona o refresh token).</summary>
        /// <response code="200">Token renovado com sucesso</response>
        /// <response code="401">Refresh token inválido ou expirado</response>
        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<SessaoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh()
        {
            // O refresh token nunca chega por JavaScript: só o navegador o conhece, via
            // cookie HttpOnly, e o anexa sozinho nesta requisição.
            if (!Request.Cookies.TryGetValue(CookiesDeSessao.Refresh, out var refreshTokenCookie))
                return Unauthorized(ApiResponse<object>.Fail("Refresh token inválido ou expirado."));

            var request = new RefreshTokenRequest { RefreshToken = refreshTokenCookie };
            var validation = await refreshValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var response = await authService.RefreshAsync(request);

            if (response is null)
                return Unauthorized(ApiResponse<object>.Fail("Refresh token inválido ou expirado."));

            SessaoCookies.Emitir(Response, response.Token, response.RefreshToken);
            return Ok(ApiResponse<SessaoResponse>.Ok(response.ToSessaoResponse(), "Token renovado com sucesso."));
        }

        /// <summary>Solicita o reset de senha; se o e-mail estiver cadastrado, envia o link por e-mail.</summary>
        /// <response code="200">Pedido registrado (resposta idêntica exista o e-mail ou não)</response>
        [HttpPost("esqueci-senha")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request)
        {
            var validation = await esqueciSenhaValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            await authService.EsqueciSenhaAsync(request);

            return Ok(ApiResponse<object>.Ok(null!,
                "Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha."));
        }

        /// <summary>Redefine a senha a partir do token recebido por e-mail; sessões antigas são encerradas.</summary>
        /// <response code="200">Senha redefinida com sucesso</response>
        /// <response code="400">Token inválido, expirado ou dados inválidos</response>
        [HttpPost("redefinir-senha")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaRequest request)
        {
            var validation = await redefinirSenhaValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var redefinida = await authService.RedefinirSenhaAsync(request);

            if (!redefinida)
                return BadRequest(ApiResponse<object>.Fail("Token de redefinição inválido ou expirado."));

            return Ok(ApiResponse<object>.Ok(null!, "Senha redefinida com sucesso. Faça login com a nova senha."));
        }

        /// <summary>Encerra a sessão do usuário, revogando o refresh token.</summary>
        /// <response code="200">Logout realizado com sucesso</response>
        /// <response code="401">Não autenticado</response>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

            if (!int.TryParse(sub, out var usuarioId))
                return Unauthorized(ApiResponse<object>.Fail("Não autorizado."));

            await authService.LogoutAsync(usuarioId);
            SessaoCookies.Limpar(Response);

            return Ok(ApiResponse<object>.Ok(null!, "Logout realizado com sucesso."));
        }
    }
}
