using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.UseCases.Veiculos.Commands.CreateVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.UpdateVeiculo;
using Frota360.Application.UseCases.Veiculos.Queries.GetAllVeiculos;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class VeiculoController(IDispatcher dispatcher,
                                IValidator<CreateVeiculoRequest> createValidator,
                                IValidator<UpdateVeiculoRequest> updateValidator) : ControllerBase
    {
        /// <summary>Retorna todos os veículos da frota.</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var veiculos = await dispatcher.SendAsync(new GetAllVeiculosQuery());
            return Ok(ApiResponse<object>.Ok(veiculos));
        }

        /// <summary>Cadastra um novo veículo.</summary>
        /// <response code="201">Veículo criado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateVeiculoRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateVeiculoCommand(request));
            return CreatedAtAction(nameof(GetAll), new { id = criado.Id },
                ApiResponse<object>.Ok(criado, "Veículo cadastrado com sucesso."));
        }

        /// <summary>Atualiza os dados de um veículo.</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="404">Veículo não encontrado</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVeiculoRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdateVeiculoCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Veículo {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(atualizado, "Veículo atualizado com sucesso."));
        }

        /// <summary>Remove um veículo da frota.</summary>
        /// <response code="204">Removido com sucesso</response>
        /// <response code="404">Veículo não encontrado</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteVeiculoCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Veículo {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Veículo removido com sucesso."));
        }
    }
}
