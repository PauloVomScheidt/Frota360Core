using Asp.Versioning;
using FluentValidation;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RotaController(IRotaService service,
                                IValidator<CreateRotaRequest> createValidator,
                                IValidator<UpdateRotaRequest> updateValidator) : ControllerBase
    {
        /// <summary>Retorna todas as rotas.</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var rotas = await service.GetAllAsync();
            return Ok(ApiResponse<object>.Ok(rotas));
        }

        /// <summary>Cadastra uma nova rota.</summary>
        /// <response code="201">Rota criada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateRotaRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await service.AddAsync(request);
            return CreatedAtAction(nameof(GetAll), new { id = criado.Id },
                ApiResponse<object>.Ok(criado, "Rota cadastrada com sucesso."));
        }

        /// <summary>Atualiza os dados de uma rota.</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRotaRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await service.UpdateAsync(id, request);

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Rota {id} não encontrada."));

            return Ok(ApiResponse<object>.Ok(atualizado, "Rota atualizada com sucesso."));
        }

        /// <summary>Remove uma rota.</summary>
        /// <response code="204">Removido com sucesso</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await service.DeleteAsync(id);

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Rota {id} não encontrada."));

            return Ok(ApiResponse<object>.Ok(null!, "Rota removida com sucesso."));
        }
    }
}
