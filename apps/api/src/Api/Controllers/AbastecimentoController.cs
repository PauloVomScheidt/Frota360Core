using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Request;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.UseCases.Abastecimentos.Commands.CreateAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Commands.DeleteAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Commands.UpdateAbastecimento;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoById;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    // Aberto a todos os papéis: quem abastece na estrada é o motorista, e quem abastece no
    // pátio costuma ser o operador. O recorte de quem vê o quê não está no atributo, e sim
    // no handler — o motorista enxerga e corrige só o que é dele, e lança sempre em si mesmo.
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AbastecimentoController(IDispatcher dispatcher,
                                         IValidator<CreateAbastecimentoRequest> createValidator,
                                         IValidator<UpdateAbastecimentoRequest> updateValidator) : ControllerBase
    {
        /// <summary>
        /// Lista os abastecimentos, opcionalmente por veículo, motorista e período. A gestão
        /// vê a frota inteira; o motorista, só o que é dele.
        /// </summary>
        /// <param name="veiculoId">Restringe a um veículo.</param>
        /// <param name="motoristaId">Restringe a um motorista. Ignorado para a role Motorista, que já é recortada pelo token.</param>
        /// <param name="de">Início do período (data do abastecimento).</param>
        /// <param name="ate">Fim do período, inclusivo.</param>
        /// <response code="200">Lista retornada com sucesso</response>
        /// <response code="422">Data final anterior à inicial</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<AbastecimentoResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetAll([FromQuery] int? veiculoId, [FromQuery] int? motoristaId,
            [FromQuery] DateTime? de, [FromQuery] DateTime? ate)
        {
            var abastecimentos = await dispatcher.SendAsync(new GetAllAbastecimentosQuery(veiculoId, motoristaId, de, ate));
            return Ok(ApiResponse<IEnumerable<AbastecimentoResponse>>.Ok(abastecimentos));
        }

        /// <summary>Retorna um abastecimento pelo id.</summary>
        /// <response code="200">Abastecimento retornado com sucesso</response>
        /// <response code="404">Não encontrado — ou é de outra pessoa, para o motorista</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<AbastecimentoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var abastecimento = await dispatcher.SendAsync(new GetAbastecimentoByIdQuery(id));

            if (abastecimento is null)
                return NotFound(ApiResponse<object>.Fail($"Abastecimento {id} não encontrado."));

            return Ok(ApiResponse<AbastecimentoResponse>.Ok(abastecimento));
        }

        /// <summary>
        /// Lança um abastecimento. (todos os papéis, inclusive Motorista) A gestão escolhe o
        /// motorista; o motorista lança sempre em si mesmo e, tendo rota aberta, só no veículo
        /// dela. A rota é vinculada pelo servidor, não pelo cliente.
        /// </summary>
        /// <response code="201">Abastecimento lançado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="422">Veículo ou motorista não encontrados, motorista não informado, ou veículo diferente do da rota aberta</response>
        [HttpPost]
        [ProducesResponseType<ApiResponse<AbastecimentoResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreateAbastecimentoRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateAbastecimentoCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<AbastecimentoResponse>.Ok(criado, "Abastecimento lançado com sucesso."));
        }

        /// <summary>
        /// Corrige um lançamento — errar o valor digitado no posto é comum. O motorista
        /// corrige os próprios; a gestão, qualquer um. Veículo, motorista e rota não são editáveis.
        /// </summary>
        /// <response code="200">Corrigido com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Não encontrado — ou é de outro motorista, para a role Motorista</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType<ApiResponse<AbastecimentoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAbastecimentoRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdateAbastecimentoCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Abastecimento {id} não encontrado."));

            return Ok(ApiResponse<AbastecimentoResponse>.Ok(atualizado, "Abastecimento corrigido com sucesso."));
        }

        /// <summary>Remove um abastecimento. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Abastecimento não encontrado</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteAbastecimentoCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Abastecimento {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Abastecimento removido com sucesso."));
        }
    }
}
