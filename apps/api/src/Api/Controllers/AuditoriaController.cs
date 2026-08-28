using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Auditoria.Request;
using Frota360.Application.DTOs.Auditoria.Response;
using Frota360.Application.UseCases.Auditoria.Queries.GetLogsAuditoria;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    /// <summary>
    /// Trilha de auditoria da empresa. Somente leitura: não há endpoint que altere ou
    /// apague uma linha — nem para o Admin.
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuditoriaController(IDispatcher dispatcher,
                                     IValidator<ConsultarAuditoriaRequest> consultarValidator) : ControllerBase
    {
        /// <summary>Lista paginada das alterações feitas na empresa, mais recentes primeiro. (Admin)</summary>
        /// <response code="200">Página retornada com sucesso</response>
        /// <response code="400">Filtro inválido</response>
        /// <response code="403">Sem permissão</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<ResultadoPaginado<LogAuditoriaResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Consultar([FromQuery] ConsultarAuditoriaRequest request)
        {
            var validation = await consultarValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Filtro inválido.", erros));
            }

            var pagina = await dispatcher.SendAsync(new GetLogsAuditoriaQuery(request));
            return Ok(ApiResponse<ResultadoPaginado<LogAuditoriaResponse>>.Ok(pagina));
        }
    }
}
