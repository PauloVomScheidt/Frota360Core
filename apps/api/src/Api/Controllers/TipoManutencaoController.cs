using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoManutencao.Request;
using Frota360.Application.DTOs.TipoManutencao.Response;
using Frota360.Application.UseCases.TiposManutencao.Commands.CreateTipoManutencao;
using Frota360.Application.UseCases.TiposManutencao.Commands.DeleteTipoManutencao;
using Frota360.Application.UseCases.TiposManutencao.Commands.UpdateTipoManutencao;
using Frota360.Application.UseCases.TiposManutencao.Queries.GetAllTiposManutencao;
using Frota360.Application.UseCases.TiposManutencao.Queries.GetTipoManutencaoById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TipoManutencaoController(IDispatcher dispatcher,
                                          IValidator<CreateTipoManutencaoRequest> createValidator,
                                          IValidator<UpdateTipoManutencaoRequest> updateValidator) : ControllerBase
    {
        /// <summary>Catálogo de tipos de manutenção da empresa. Alimenta o seletor da tela de manutenção.</summary>
        /// <param name="apenasAtivos">Use true no dropdown de agendamento para esconder tipos aposentados.</param>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<TipoManutencaoResponse>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool apenasAtivos = false)
        {
            var tipos = await dispatcher.SendAsync(new GetAllTiposManutencaoQuery(apenasAtivos));
            return Ok(ApiResponse<IEnumerable<TipoManutencaoResponse>>.Ok(tipos));
        }

        /// <summary>Retorna um tipo de manutenção pelo id.</summary>
        /// <response code="200">Tipo retornado com sucesso</response>
        /// <response code="404">Tipo não encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<TipoManutencaoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var tipo = await dispatcher.SendAsync(new GetTipoManutencaoByIdQuery(id));

            if (tipo is null)
                return NotFound(ApiResponse<object>.Fail($"Tipo de manutenção {id} não encontrado."));

            return Ok(ApiResponse<TipoManutencaoResponse>.Ok(tipo));
        }

        /// <summary>Cadastra um tipo de manutenção. (Admin, Supervisor)</summary>
        /// <response code="201">Tipo criado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Já existe tipo com esse nome</response>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<TipoManutencaoResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreateTipoManutencaoRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateTipoManutencaoCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<TipoManutencaoResponse>.Ok(criado, "Tipo de manutenção cadastrado com sucesso."));
        }

        /// <summary>Atualiza um tipo de manutenção. (Admin, Supervisor)</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Já existe tipo com esse nome</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<TipoManutencaoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTipoManutencaoRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdateTipoManutencaoCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Tipo de manutenção {id} não encontrado."));

            return Ok(ApiResponse<TipoManutencaoResponse>.Ok(atualizado, "Tipo de manutenção atualizado com sucesso."));
        }

        /// <summary>Remove um tipo de manutenção ainda não utilizado. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Tipo em uso por manutenções; inative-o em vez de excluir</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteTipoManutencaoCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Tipo de manutenção {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Tipo de manutenção removido com sucesso."));
        }
    }
}
