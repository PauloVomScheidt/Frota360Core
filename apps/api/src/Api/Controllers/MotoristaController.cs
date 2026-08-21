using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.UseCases.Motoristas.Commands.CreateMotorista;
using Frota360.Application.UseCases.Motoristas.Commands.DeleteMotorista;
using Frota360.Application.UseCases.Motoristas.Commands.UpdateMotorista;
using Frota360.Application.UseCases.Motoristas.Queries.GetAllMotoristas;
using Frota360.Application.UseCases.Motoristas.Queries.GetMotoristaById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class MotoristaController(IDispatcher dispatcher,
                                IValidator<CreateMotoristaRequest> createValidator,
                                IValidator<UpdateMotoristaRequest> updateValidator) : ControllerBase
    {
        /// <summary>Retorna todas os motoristas.</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<MotoristaResponse>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var motoristas = await dispatcher.SendAsync(new GetAllMotoristasQuery());
            return Ok(ApiResponse<IEnumerable<MotoristaResponse>>.Ok(motoristas));
        }

        /// <summary>Retorna um motorista pelo id.</summary>
        /// <response code="200">Motorista retornado com sucesso</response>
        /// <response code="404">Motorista não encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<MotoristaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var motorista = await dispatcher.SendAsync(new GetMotoristaByIdQuery(id));

            if (motorista is null)
                return NotFound(ApiResponse<object>.Fail($"Motorista {id} não encontrado."));

            return Ok(ApiResponse<MotoristaResponse>.Ok(motorista));
        }

        /// <summary>Cadastra um novo motorista. (Admin, Supervisor)</summary>
        /// <response code="201">Motorista criado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<MotoristaResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateMotoristaRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateMotoristaCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<MotoristaResponse>.Ok(criado, "Motorista cadastrado com sucesso."));
        }

        /// <summary>Atualiza os dados de um motorista. (Admin, Supervisor)</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Motorista não encontrado</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<MotoristaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMotoristaRequest request)
        {
            var validation = await updateValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var atualizado = await dispatcher.SendAsync(new UpdateMotoristaCommand(id, request));

            if (atualizado is null)
                return NotFound(ApiResponse<object>.Fail($"Motorista {id} não encontrado."));

            return Ok(ApiResponse<MotoristaResponse>.Ok(atualizado, "Motorista atualizado com sucesso."));
        }

        /// <summary>Remove um motorista. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Motorista não encontrado</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteMotoristaCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Motorista {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Motorista removido com sucesso."));
        }
    }
}
