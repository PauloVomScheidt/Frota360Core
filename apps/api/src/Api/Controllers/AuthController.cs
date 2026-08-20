using Asp.Versioning;
using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
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
                            IUsuarioRepository usuarioRepository,
                            IValidator<RegisterRequest> registerValidator,
                            IValidator<LoginRequest> loginValidator,
                            IValidator<RefreshTokenRequest> refreshValidator) : ControllerBase
    {
        /// <summary>Registra um novo usuário.</summary>
        /// <response code="201">Usuário criado com sucesso</response>
        /// <response code="400">Dados inválidos ou e-mail já cadastrado</response>
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var validation = await registerValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            if (await usuarioRepository.ExisteEmailAsync(request.Email))
                return BadRequest(ApiResponse<object>.Fail("E-mail já cadastrado."));

            var response = await authService.RegisterAsync(request);
            return Created(string.Empty, ApiResponse<AuthResponse>.Ok(response, "Usuário cadastrado com sucesso."));
        }

        /// <summary>Autentica um usuário e retorna o token JWT e o refresh token.</summary>
        /// <response code="200">Login realizado com sucesso</response>
        /// <response code="401">Credenciais inválidas</response>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
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

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Login realizado com sucesso."));
        }

        /// <summary>Renova o token JWT a partir de um refresh token válido (rotaciona o refresh token).</summary>
        /// <response code="200">Token renovado com sucesso</response>
        /// <response code="401">Refresh token inválido ou expirado</response>
        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var validation = await refreshValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var response = await authService.RefreshAsync(request);

            if (response is null)
                return Unauthorized(ApiResponse<object>.Fail("Refresh token inválido ou expirado."));

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Token renovado com sucesso."));
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

            return Ok(ApiResponse<object>.Ok(null!, "Logout realizado com sucesso."));
        }
    }
}
