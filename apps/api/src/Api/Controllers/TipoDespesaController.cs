using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoDespesa.Request;
using Frota360.Application.DTOs.TipoDespesa.Response;
using Frota360.Application.UseCases.TiposDespesa.Commands.CreateTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Commands.DeleteTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Commands.UpdateTipoDespesa;
using Frota360.Application.UseCases.TiposDespesa.Queries.GetAllTiposDespesa;
using Frota360.Application.UseCases.TiposDespesa.Queries.GetTipoDespesaById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    // Catálogo só serve à tela de despesas, que o motorista não acessa.
    [Authorize(Roles = Roles.Gestao)]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TipoDespesaController(IDispatcher dispatcher,
                                       IValidator<CreateTipoDespesaRequest> createValidator,
                                       IValidator<UpdateTipoDespesaRequest> updateValidator) : ControllerBase
    {
        /// <summary>Catálogo de tipos de despesa da empresa. Alimenta o seletor da tela de despesas.</summary>
        /// <param name="apenasAtivos">Use true no seletor de lançamento para esconder tipos aposentados.</param>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<TipoDespesaResponse>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool apenasAtivos = false)
        {
            var tipos = await dispatcher.SendAsync(new GetAllTiposDespesaQuery(apenasAtivos));
            return Ok(ApiResponse<IEnumerable<TipoDespesaResponse>>.Ok(tipos));
        }

        /// <summary>Retorna um tipo de despesa pelo id.</summary>
        /// <response code="200">Tipo retornado com sucesso</response>
        /// <response code="404">Tipo não encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<TipoDespesaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var tipo = await dispatcher.SendAsync(new GetTipoDespesaByIdQuery(id));

            if (tipo is null)
                return NotFound(ApiResponse<object>.Fail($"Tipo de despesa {id} não encontrado."));

            return Ok(ApiResponse<TipoDespesaResponse>.Ok(tipo));
        }

        /// <summary>Cadastra um tipo de despesa. (Admin, Supervisor)</summary>
        /// <response code="201">Tipo criado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Já existe tipo com esse nome</response>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<TipoDespesaResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreateTipoDespesaRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateTipoDespesaCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<TipoDespesaResponse>.Ok(criado, "Tipo de despesa cadastrado com sucesso."));
        }

        /// <summary>Atualiza um tipo de despesa. (Admin, Supervisor)</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Já existe tipo com esse nome</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<TipoDespesaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTipoDespesaRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdateTipoDespesaCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Tipo de despesa {id} não encontrado."));

            return Ok(ApiResponse<TipoDespesaResponse>.Ok(atualizado, "Tipo de despesa atualizado com sucesso."));
        }

        /// <summary>Remove um tipo de despesa ainda não utilizado. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Tipo em uso por despesas; inative-o em vez de excluir</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteTipoDespesaCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Tipo de despesa {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Tipo de despesa removido com sucesso."));
        }
    }
}
