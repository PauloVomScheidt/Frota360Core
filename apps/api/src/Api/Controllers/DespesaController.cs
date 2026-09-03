using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Despesa.Request;
using Frota360.Application.DTOs.Despesa.Response;
using Frota360.Application.UseCases.Despesas.Commands.CreateDespesa;
using Frota360.Application.UseCases.Despesas.Commands.DeleteDespesa;
using Frota360.Application.UseCases.Despesas.Commands.UpdateDespesa;
using Frota360.Application.UseCases.Despesas.Queries.GetAllDespesas;
using Frota360.Application.UseCases.Despesas.Queries.GetDespesaById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    /// <summary>
    /// Custos avulsos: pedágio, multa, IPVA, seguro, licenciamento. É a terceira origem da
    /// tela de custos, e a única cuja tabela é fonte de verdade — as outras duas são lidas
    /// das telas de abastecimento e manutenção.
    ///
    /// Fechado na gestão: o lançamento é administrativo, e o Motorista não vê valor de frota.
    /// </summary>
    [Authorize(Roles = Roles.Gestao)]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DespesaController(IDispatcher dispatcher,
                                   IValidator<CreateDespesaRequest> createValidator,
                                   IValidator<UpdateDespesaRequest> updateValidator) : ControllerBase
    {
        /// <summary>Despesas da empresa, das mais recentes para as mais antigas. (Gestão)</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Período com data final anterior à inicial</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<DespesaResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetAll([FromQuery] int? veiculoId, [FromQuery] int? motoristaId,
            [FromQuery] int? tipoDespesaId, [FromQuery] DateTime? de, [FromQuery] DateTime? ate)
        {
            var despesas = await dispatcher.SendAsync(
                new GetAllDespesasQuery(veiculoId, motoristaId, tipoDespesaId, de, ate));

            return Ok(ApiResponse<IEnumerable<DespesaResponse>>.Ok(despesas));
        }

        /// <summary>Retorna uma despesa pelo id.</summary>
        /// <response code="200">Despesa retornada com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Despesa não encontrada</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<DespesaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var despesa = await dispatcher.SendAsync(new GetDespesaByIdQuery(id));

            if (despesa is null)
                return NotFound(ApiResponse<object>.Fail($"Despesa {id} não encontrada."));

            return Ok(ApiResponse<DespesaResponse>.Ok(despesa));
        }

        /// <summary>Lança uma despesa. (Gestão)</summary>
        /// <response code="201">Despesa lançada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Veículo, tipo ou motorista não encontrado, ou tipo inativo</response>
        [HttpPost]
        [ProducesResponseType<ApiResponse<DespesaResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreateDespesaRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criada = await dispatcher.SendAsync(new CreateDespesaCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criada.Id },
                ApiResponse<DespesaResponse>.Ok(criada, "Despesa lançada com sucesso."));
        }

        /// <summary>
        /// Corrige uma despesa. Diferente do abastecimento, a correção alcança **todos** os
        /// campos — não há recorte por dono que a troca de veículo ou motorista burlaria.
        /// </summary>
        /// <response code="200">Despesa atualizada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Despesa não encontrada</response>
        /// <response code="422">Veículo, tipo ou motorista não encontrado, ou tipo inativo</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType<ApiResponse<DespesaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDespesaRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizada = await dispatcher.SendAsync(new UpdateDespesaCommand(id, request));

            if (atualizada is null)
                return NotFound(ApiResponse<object>.Fail($"Despesa {id} não encontrada."));

            return Ok(ApiResponse<DespesaResponse>.Ok(atualizada, "Despesa atualizada com sucesso."));
        }

        /// <summary>
        /// Exclui uma despesa. (Admin, Supervisor)
        ///
        /// ⚠️ Exceção deliberada à regra "Admin é o único que exclui" que vale no resto da
        /// API: aqui o Supervisor também exclui, por decisão de produto. Não "corrija" isto
        /// achando que é descuido — está registrado em apps/api/CLAUDE.md.
        /// </summary>
        /// <response code="200">Despesa removida com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Despesa não encontrada</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletada = await dispatcher.SendAsync(new DeleteDespesaCommand(id));

            if (!deletada)
                return NotFound(ApiResponse<object>.Fail($"Despesa {id} não encontrada."));

            return Ok(ApiResponse<object>.Ok(null!, "Despesa removida com sucesso."));
        }
    }
}
