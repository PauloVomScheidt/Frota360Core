using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService,
                            IUsuarioRepository usuarioRepository,
                            IValidator<RegisterRequest> registerValidator,
                            IValidator<LoginRequest> loginValidator) : ControllerBase
    {
        /// <summary>Registra um novo usuário.</summary>
        /// <response code="201">Usuário criado com sucesso</response>
        /// <response code="400">Dados inválidos ou e-mail já cadastrado</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var validation = await registerValidator.ValidateAsync(request);

            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => new
                {
                    campo = e.PropertyName,
                    erro = e.ErrorMessage
                }));

            var emailExiste = await usuarioRepository.ExisteEmailAsync(request.Email);

            if (emailExiste)
                return BadRequest(new { erro = "E-mail já cadastrado." });

            var response = await authService.RegisterAsync(request);
            return Created(string.Empty, response);
        }

        /// <summary>Autentica um usuário e retorna o token JWT.</summary>
        /// <response code="200">Login realizado com sucesso</response>
        /// <response code="401">Credenciais inválidas</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validation = await loginValidator.ValidateAsync(request);

            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => new
                {
                    campo = e.PropertyName,
                    erro = e.ErrorMessage
                }));

            var response = await authService.LoginAsync(request);

            if (response is null)
                return Unauthorized(new { erro = "E-mail ou senha inválidos." });

            return Ok(response);
        }
    }
}