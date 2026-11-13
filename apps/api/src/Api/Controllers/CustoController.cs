using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.DTOs.Custo.Response;
using Frota360.Application.UseCases.Custos.Queries.GetCustos;
using Frota360.Application.UseCases.Custos.Queries.GetResumoCustos;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    /// <summary>
    /// Visão consolidada do que a frota custou. Somente leitura: não há tabela de custos —
    /// o valor continua sendo lançado no abastecimento e na conclusão da manutenção, e aqui
    /// as duas origens são apenas unidas.
    ///
    /// <c>Roles.Gestao</c> barra o Motorista na porta, e é por isso que os handlers não
    /// replicam a regra de <c>ManutencaoVisibilidade.SemCustoParaMotorista</c>. Se um dia esta
    /// tela abrir para a role Motorista, o recorte tem que voltar para dentro dos handlers.
    /// </summary>
    [Authorize(Roles = Roles.Gestao)]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class CustoController(IDispatcher dispatcher,
                                 IValidator<ConsultarCustosRequest> consultarValidator,
                                 IValidator<ResumoCustosRequest> resumoValidator) : ControllerBase
    {
        /// <summary>Lista paginada dos custos da empresa, mais recentes primeiro. (Gestão)</summary>
        /// <response code="200">Página retornada com sucesso</response>
        /// <response code="400">Filtro inválido</response>
        /// <response code="403">Sem permissão</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<ResultadoPaginado<LancamentoCustoResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Consultar([FromQuery] ConsultarCustosRequest request)
        {
            var validation = await consultarValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Filtro inválido.", erros));
            }

            var pagina = await dispatcher.SendAsync(new GetCustosQuery(request));
            return Ok(ApiResponse<ResultadoPaginado<LancamentoCustoResponse>>.Ok(pagina));
        }

        /// <summary>Totais do período, somados no banco: por origem, por veículo e por mês. (Gestão)</summary>
        /// <response code="200">Resumo retornado com sucesso</response>
        /// <response code="400">Filtro inválido</response>
        /// <response code="403">Sem permissão</response>
        [HttpGet("resumo")]
        [ProducesResponseType<ApiResponse<ResumoCustosResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Resumir([FromQuery] ResumoCustosRequest request)
        {
            var validation = await resumoValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Filtro inválido.", erros));
            }

            var resumo = await dispatcher.SendAsync(new GetResumoCustosQuery(request));
            return Ok(ApiResponse<ResumoCustosResponse>.Ok(resumo));
        }
    }
}
