using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.TipoCombustivel.Request;
using Frota360.Application.DTOs.TipoCombustivel.Response;
using Frota360.Application.UseCases.TiposCombustivel.Commands.CreateTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Commands.DeleteTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Commands.UpdateTipoCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Queries.GetAllTiposCombustivel;
using Frota360.Application.UseCases.TiposCombustivel.Queries.GetTipoCombustivelById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    // Leitura aberta a todos os papéis, escrita só à gestão: quem lança abastecimento
    // inclui o motorista, e ele precisa do catálogo para preencher o formulário. Por isso
    // Roles.Gestao fica nas ações de escrita e não na classe — os atributos se combinam
    // por E, e na classe ele barraria também o GET.
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TipoCombustivelController(IDispatcher dispatcher,
                                       IValidator<CreateTipoCombustivelRequest> createValidator,
                                       IValidator<UpdateTipoCombustivelRequest> updateValidator) : ControllerBase
    {
        /// <summary>Catálogo de tipos de combustível da empresa. Alimenta o seletor da tela de abastecimentos.</summary>
        /// <param name="apenasAtivos">Use true no seletor de lançamento para esconder tipos aposentados.</param>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<TipoCombustivelResponse>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool apenasAtivos = false)
        {
            var tipos = await dispatcher.SendAsync(new GetAllTiposCombustivelQuery(apenasAtivos));
            return Ok(ApiResponse<IEnumerable<TipoCombustivelResponse>>.Ok(tipos));
        }

        /// <summary>Retorna um tipo de combustível pelo id.</summary>
        /// <response code="200">Tipo retornado com sucesso</response>
        /// <response code="404">Tipo não encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<TipoCombustivelResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var tipo = await dispatcher.SendAsync(new GetTipoCombustivelByIdQuery(id));

            if (tipo is null)
                return NotFound(ApiResponse<object>.Fail($"Tipo de combustível {id} não encontrado."));

            return Ok(ApiResponse<TipoCombustivelResponse>.Ok(tipo));
        }

        /// <summary>Cadastra um tipo de combustível. (Admin, Supervisor)</summary>
        /// <response code="201">Tipo criado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Já existe tipo com esse nome</response>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<TipoCombustivelResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreateTipoCombustivelRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateTipoCombustivelCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<TipoCombustivelResponse>.Ok(criado, "Tipo de combustível cadastrado com sucesso."));
        }

        /// <summary>Atualiza um tipo de combustível. (Admin, Supervisor)</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Já existe tipo com esse nome</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<TipoCombustivelResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTipoCombustivelRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdateTipoCombustivelCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Tipo de combustível {id} não encontrado."));

            return Ok(ApiResponse<TipoCombustivelResponse>.Ok(atualizado, "Tipo de combustível atualizado com sucesso."));
        }

        /// <summary>Remove um tipo de combustível ainda não utilizado. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Combustível em uso por abastecimentos; inative-o em vez de excluir</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteTipoCombustivelCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Tipo de combustível {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Tipo de combustível removido com sucesso."));
        }
    }
}
