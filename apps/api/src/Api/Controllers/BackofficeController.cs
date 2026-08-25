using Asp.Versioning;
using FluentValidation;
using Frota360.Application.DTOs.Backoffice.Request;
using Frota360.Application.DTOs.Backoffice.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frota360.Api.Controllers
{
    /// <summary>
    /// Operações internas de venda assistida, protegidas por API key (header X-Backoffice-Key).
    /// Sem Backoffice:ApiKey configurada, os endpoints ficam desabilitados (401).
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/backoffice")]
    public class BackofficeController(IBackofficeService backofficeService,
                                      IConfiguration configuration,
                                      IValidator<ProvisionarEmpresaRequest> validator) : ControllerBase
    {
        /// <summary>Provisiona uma empresa nova e envia o convite do primeiro administrador.</summary>
        /// <response code="201">Empresa criada e convite enviado</response>
        /// <response code="401">API key ausente ou inválida</response>
        /// <response code="422">CNPJ ou e-mail já cadastrado</response>
        [HttpPost("empresa")]
        [EnableRateLimiting("auth")]
        [ProducesResponseType<ApiResponse<EmpresaProvisionadaResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ProvisionarEmpresa([FromBody] ProvisionarEmpresaRequest request)
        {
            var apiKey = configuration["Backoffice:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey) ||
                !Request.Headers.TryGetValue("X-Backoffice-Key", out var chaveInformada) ||
                chaveInformada != apiKey)
            {
                return Unauthorized(ApiResponse<object>.Fail("Não autorizado."));
            }

            var validation = await validator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var response = await backofficeService.ProvisionarEmpresaAsync(request);
            return Created(string.Empty,
                ApiResponse<EmpresaProvisionadaResponse>.Ok(response, "Empresa provisionada com sucesso."));
        }
    }
}
