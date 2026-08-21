using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.UseCases.Rotas.Commands.CreateRota;
using Frota360.Application.UseCases.Rotas.Commands.DeleteRota;
using Frota360.Application.UseCases.Rotas.Commands.UpdateRota;
using Frota360.Application.UseCases.Rotas.Queries.GetAllRotas;
using Frota360.Application.UseCases.Rotas.Queries.GetRotaById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RotaController(IDispatcher dispatcher,
                                IValidator<CreateRotaRequest> createValidator,
                                IValidator<UpdateRotaRequest> updateValidator) : ControllerBase
    {
        /// <summary>Retorna todas as rotas.</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<RotaResponse>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var rotas = await dispatcher.SendAsync(new GetAllRotasQuery());
            return Ok(ApiResponse<IEnumerable<RotaResponse>>.Ok(rotas));
        }

        /// <summary>Retorna uma rota pelo id.</summary>
        /// <response code="200">Rota retornada com sucesso</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<RotaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var rota = await dispatcher.SendAsync(new GetRotaByIdQuery(id));

            if (rota is null)
                return NotFound(ApiResponse<object>.Fail($"Rota {id} não encontrada."));

            return Ok(ApiResponse<RotaResponse>.Ok(rota));
        }

        /// <summary>Cadastra uma nova rota.</summary>
        /// <response code="201">Rota criada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        [HttpPost]
        [ProducesResponseType<ApiResponse<RotaResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateRotaRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateRotaCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<RotaResponse>.Ok(criado, "Rota cadastrada com sucesso."));
        }

        /// <summary>Atualiza os dados de uma rota.</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType<ApiResponse<RotaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRotaRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdateRotaCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Rota {id} não encontrada."));

            return Ok(ApiResponse<RotaResponse>.Ok(atualizado, "Rota atualizada com sucesso."));
        }

        /// <summary>Remove uma rota.</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteRotaCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Rota {id} não encontrada."));

            return Ok(ApiResponse<object>.Ok(null!, "Rota removida com sucesso."));
        }
    }
}
