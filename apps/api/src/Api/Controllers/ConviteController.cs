using Asp.Versioning;
using FluentValidation;
using Frota360.Application.DTOs.Convite.Request;
using Frota360.Application.DTOs.Convite.Response;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frota360.Api.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ConviteController(IConviteService conviteService,
                                   IValidator<CriarConviteRequest> criarValidator,
                                   IValidator<AceitarConviteRequest> aceitarValidator) : ControllerBase
    {
        /// <summary>Lista os convites da empresa. (Admin)</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<ConviteResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Listar()
        {
            var convites = await conviteService.ListarAsync();
            return Ok(ApiResponse<IEnumerable<ConviteResponse>>.Ok(convites));
        }

        /// <summary>Convida uma pessoa para a empresa; o link é enviado por e-mail. (Admin)</summary>
        /// <response code="201">Convite criado e e-mail enviado</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="422">E-mail já cadastrado</response>
        [HttpPost]
        [ProducesResponseType<ApiResponse<ConviteCriadoResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Criar([FromBody] CriarConviteRequest request)
        {
            var validation = await criarValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var convite = await conviteService.CriarAsync(request);
            return Created(string.Empty, ApiResponse<ConviteCriadoResponse>.Ok(convite, "Convite enviado com sucesso."));
        }

        /// <summary>Cancela um convite pendente. (Admin)</summary>
        /// <response code="200">Convite cancelado</response>
        /// <response code="404">Convite não encontrado</response>
        /// <response code="422">Convite já utilizado</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancelar(int id)
        {
            var cancelado = await conviteService.CancelarAsync(id);

            if (!cancelado)
                return NotFound(ApiResponse<object>.Fail($"Convite {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Convite cancelado com sucesso."));
        }

        /// <summary>Aceita um convite: cria a conta na empresa/perfil do convite e já autentica.</summary>
        /// <response code="200">Conta criada com sucesso</response>
        /// <response code="400">Convite inválido, expirado ou dados inválidos</response>
        [AllowAnonymous]
        [HttpPost("aceitar")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Aceitar([FromBody] AceitarConviteRequest request)
        {
            var validation = await aceitarValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var response = await conviteService.AceitarAsync(request);

            if (response is null)
                return BadRequest(ApiResponse<object>.Fail("Convite inválido ou expirado."));

            return Ok(ApiResponse<AuthResponse>.Ok(response, "Conta criada com sucesso."));
        }
    }
}
