using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Request;
using Frota360.Application.DTOs.Posto.Response;
using Frota360.Application.UseCases.Postos.Commands.CreatePosto;
using Frota360.Application.UseCases.Postos.Commands.DeletePosto;
using Frota360.Application.UseCases.Postos.Commands.UpdatePosto;
using Frota360.Application.UseCases.Postos.Queries.GetAllPostos;
using Frota360.Application.UseCases.Postos.Queries.GetPostoById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    // Mesma regra do catálogo de combustível: o motorista lê a rede credenciada para
    // lançar, mas quem a mantém é a gestão.
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PostoController(IDispatcher dispatcher,
                                 IValidator<CreatePostoRequest> createValidator,
                                 IValidator<UpdatePostoRequest> updateValidator) : ControllerBase
    {
        /// <summary>Catálogo de postos da empresa. Alimenta o seletor da tela de abastecimentos.</summary>
        /// <param name="apenasAtivos">Use true no seletor de lançamento para esconder postos descredenciados.</param>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<PostoResponse>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool apenasAtivos = false)
        {
            var postos = await dispatcher.SendAsync(new GetAllPostosQuery(apenasAtivos));
            return Ok(ApiResponse<IEnumerable<PostoResponse>>.Ok(postos));
        }

        /// <summary>Retorna um posto pelo id.</summary>
        /// <response code="200">Tipo retornado com sucesso</response>
        /// <response code="404">Tipo não encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<PostoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var tipo = await dispatcher.SendAsync(new GetPostoByIdQuery(id));

            if (tipo is null)
                return NotFound(ApiResponse<object>.Fail($"Posto {id} não encontrado."));

            return Ok(ApiResponse<PostoResponse>.Ok(tipo));
        }

        /// <summary>Cadastra um posto. (Admin, Supervisor)</summary>
        /// <response code="201">Tipo criado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Já existe posto com esse nome</response>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<PostoResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Create([FromBody] CreatePostoRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreatePostoCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<PostoResponse>.Ok(criado, "Posto cadastrado com sucesso."));
        }

        /// <summary>Atualiza um posto. (Admin, Supervisor)</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Já existe posto com esse nome</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<PostoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePostoRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdatePostoCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Posto {id} não encontrado."));

            return Ok(ApiResponse<PostoResponse>.Ok(atualizado, "Posto atualizado com sucesso."));
        }

        /// <summary>Remove um posto ainda não utilizado. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Tipo não encontrado</response>
        /// <response code="422">Posto em uso por abastecimentos; inative-o em vez de excluir</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeletePostoCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Posto {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Posto removido com sucesso."));
        }
    }
}
