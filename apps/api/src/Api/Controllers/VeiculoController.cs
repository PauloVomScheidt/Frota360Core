using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.UseCases.Veiculos.Commands.CreateVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.UpdateVeiculo;
using Frota360.Application.UseCases.Veiculos.Queries.GetAllVeiculos;
using Frota360.Application.UseCases.Veiculos.Queries.GetVeiculoById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    // Leitura aberta a todos os papéis, motorista incluído: ele abre rota escolhendo o
    // veículo e consulta a frota em /veiculos. Escrita continua restrita.
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
        [ProducesResponseType<ApiResponse<IEnumerable<VeiculoResponse>>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var veiculos = await dispatcher.SendAsync(new GetAllVeiculosQuery());
            return Ok(ApiResponse<IEnumerable<VeiculoResponse>>.Ok(veiculos));
        }

        /// <summary>Retorna um veículo pelo id.</summary>
        /// <response code="200">Veículo retornado com sucesso</response>
        /// <response code="404">Veículo não encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<VeiculoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var veiculo = await dispatcher.SendAsync(new GetVeiculoByIdQuery(id));

            if (veiculo is null)
                return NotFound(ApiResponse<object>.Fail($"Veículo {id} não encontrado."));

            return Ok(ApiResponse<VeiculoResponse>.Ok(veiculo));
        }

        /// <summary>Cadastra um novo veículo. (Admin, Supervisor)</summary>
        /// <response code="201">Veículo criado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="403">Sem permissão</response>
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<VeiculoResponse>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] CreateVeiculoRequest request)
        {
            var validation = await createValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var criado = await dispatcher.SendAsync(new CreateVeiculoCommand(request));
            return CreatedAtAction(nameof(GetById), new { id = criado.Id },
                ApiResponse<VeiculoResponse>.Ok(criado, "Veículo cadastrado com sucesso."));
        }

        /// <summary>Atualiza os dados de um veículo. (Admin, Supervisor)</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Veículo não encontrado</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Supervisor}")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<VeiculoResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
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

            return Ok(ApiResponse<VeiculoResponse>.Ok(atualizado, "Veículo atualizado com sucesso."));
        }

        /// <summary>Remove um veículo da frota sem rotas associadas. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Veículo não encontrado</response>
        /// <response code="422">Veículo com rotas associadas (RN08); encerre ou remova as rotas antes</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Delete(int id)
        {
            var deletado = await dispatcher.SendAsync(new DeleteVeiculoCommand(id));

            if (!deletado)
                return NotFound(ApiResponse<object>.Fail($"Veículo {id} não encontrado."));

            return Ok(ApiResponse<object>.Ok(null!, "Veículo removido com sucesso."));
        }
    }
}
