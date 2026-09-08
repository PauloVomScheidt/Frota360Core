using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Request;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.UseCases.Manutencoes.Commands.ConcluirManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.CreateManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.DeleteManutencao;
using Frota360.Application.UseCases.Manutencoes.Commands.UpdateManutencao;
using Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes;
using Frota360.Application.UseCases.Manutencoes.Queries.GetManutencaoById;
using Frota360.Domain.Common;
using Frota360.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    // Leitura aberta a todos os papéis: o motorista precisa saber se o veículo que vai
    // pegar tem pendência. Escrita continua restrita, e o custo é omitido para ele
    // (ver GetAllManutencoesHandler).
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ManutencaoController(IDispatcher dispatcher,
                                      IValidator<CreateManutencaoRequest> createValidator,
                                      IValidator<UpdateManutencaoRequest> updateValidator,
                                      IValidator<ConcluirManutencaoRequest> concluirValidator,
                                      IValidator<ConsultarManutencoesRequest> consultarValidator) : ControllerBase
    {
        /// <summary>Lista as manutenções da frota, opcionalmente filtradas por veículo, status e período.</summary>
        /// <param name="veiculoId">Restringe a um veículo.</param>
        /// <param name="status">Pendente, Realizada ou Cancelada.</param>
        /// <param name="de">Início do período. Incide sobre a data prevista (pendentes) ou a de realização (concluídas).</param>
        /// <param name="ate">Fim do período, inclusivo.</param>
        /// <response code="200">Lista retornada com sucesso</response>
        /// <response code="422">Data final anterior à inicial</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<ResultadoPaginado<ManutencaoResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetAll([FromQuery] ConsultarManutencoesRequest request)
        {
            var validation = await consultarValidator.ValidateAsync(request);
            if (!validation.IsValid)
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.",
                    validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

            var pagina = await dispatcher.SendAsync(new GetAllManutencoesQuery(request));
            return Ok(ApiResponse<ResultadoPaginado<ManutencaoResponse>>.Ok(pagina));
        }

        /// <summary>Retorna uma manutenção pelo id.</summary>
        /// <response code="200">Manutenção retornada com sucesso</response>
        /// <response code="404">Manutenção não encontrada</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<ManutencaoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var manutencao = await dispatcher.SendAsync(new GetManutencaoByIdQuery(id));

            if (manutencao is null)
                return NotFound(ApiResponse<object>.Fail($"Manutenção {id} não encontrada."));

            return Ok(ApiResponse<ManutencaoResponse>.Ok(manutencao));
        }

        /// <summary>Agenda uma manutenção para um veículo da frota. (Admin, Supervisor)</summary>
        /// <response code="201">Manutenção agendada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Veículo ou tipo inexistente, tipo inativo ou agendamento duplicado</response>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<ManutencaoResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreateManutencaoRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criada = await dispatcher.SendAsync(new CreateManutencaoCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criada.Id },
                ApiResponse<ManutencaoResponse>.Ok(criada, "Manutenção agendada com sucesso."));
        }

        /// <summary>Replaneja uma manutenção ainda pendente. (Admin, Supervisor)</summary>
        /// <response code="200">Atualizada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Manutenção não encontrada</response>
        /// <response code="422">Manutenção já concluída/cancelada, ou veículo/tipo inexistente</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<ManutencaoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateManutencaoRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizada = await dispatcher.SendAsync(new UpdateManutencaoCommand(id, request));

            if (atualizada is null)
                return NotFound(ApiResponse<object>.Fail($"Manutenção {id} não encontrada."));

            return Ok(ApiResponse<ManutencaoResponse>.Ok(atualizada, "Manutenção atualizada com sucesso."));
        }

        /// <summary>
        /// Registra a execução da manutenção: km real, data, custo. A quilometragem informada
        /// também atualiza o veículo quando for maior que a atual. (Admin, Supervisor)
        /// </summary>
        /// <response code="200">Manutenção concluída com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Manutenção não encontrada</response>
        /// <response code="422">Manutenção não está pendente</response>
        [HttpPost("{id:int}/concluir")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<ManutencaoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Concluir(int id, [FromBody] ConcluirManutencaoRequest request)
        {
            var validation = await concluirValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var concluida = await dispatcher.SendAsync(new ConcluirManutencaoCommand(id, request));

            if (concluida is null)
                return NotFound(ApiResponse<object>.Fail($"Manutenção {id} não encontrada."));

            return Ok(ApiResponse<ManutencaoResponse>.Ok(concluida, "Manutenção concluída com sucesso."));
        }

        /// <summary>Remove uma manutenção. (Admin)</summary>
        /// <response code="200">Removida com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Manutenção não encontrada</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletada = await dispatcher.SendAsync(new DeleteManutencaoCommand(id));

            if (!deletada)
                return NotFound(ApiResponse<object>.Fail($"Manutenção {id} não encontrada."));

            return Ok(ApiResponse<object>.Ok(null!, "Manutenção removida com sucesso."));
        }
    }
}
