using FluentValidation;
using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VeiculoController(IVeiculoService service,
                                IValidator<CreateVeiculoRequest> createValidator,
                                IValidator<UpdateVeiculoRequest> updateValidator) : ControllerBase
    {
        /// <summary>Retorna todos os veículos da frota.</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var veiculos = await service.GetAllAsync();
            return Ok(veiculos);
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
                return BadRequest(validation.Errors.Select(e => new
                {
                    campo = e.PropertyName,
                    erro = e.ErrorMessage
                }));

            var criado = await service.AddAsync(request);
            return Ok(criado);
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
                return BadRequest(validation.Errors.Select(e => new
                {
                    campo = e.PropertyName,
                    erro = e.ErrorMessage
                }));

            var atualizado = await service.UpdateAsync(id, request);

            if (atualizado is null)
                return NotFound(new { mensagem = $"Veículo {id} não encontrado." });

            return Ok(atualizado);
        }

        /// <summary>Remove um veículo da frota.</summary>
        /// <response code="204">Removido com sucesso</response>
        /// <response code="404">Veículo não encontrado</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await service.DeleteAsync(id);

            if (!deletado)
                return NotFound(new { mensagem = $"Veículo {id} não encontrado." });

            return NoContent();
        }
    }
}
