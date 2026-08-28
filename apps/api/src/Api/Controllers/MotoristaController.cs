using Asp.Versioning;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.UseCases.Motoristas.Queries.GetAllMotoristas;
using Frota360.Application.UseCases.Motoristas.Queries.GetMotoristaById;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    /// <summary>
    /// Somente leitura: um motorista é um usuário com a role Motorista, então quem
    /// concede e remove o acesso é o fluxo de convite/usuário, não este controller.
    /// </summary>
    // Cadastro é assunto de gestão: o motorista não tem por que enumerar os colegas.
    [Authorize(Roles = Roles.Gestao)]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class MotoristaController(IDispatcher dispatcher) : ControllerBase
    {
        /// <summary>Lista os motoristas da empresa — os usuários com a role Motorista. (Admin, Supervisor, Operador)</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        /// <response code="403">Sem permissão</response>
        [HttpGet]
        [ProducesResponseType<ApiResponse<IEnumerable<MotoristaResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            var motoristas = await dispatcher.SendAsync(new GetAllMotoristasQuery());
            return Ok(ApiResponse<IEnumerable<MotoristaResponse>>.Ok(motoristas));
        }

        /// <summary>Retorna um motorista pelo id. (Admin, Supervisor, Operador)</summary>
        /// <response code="200">Motorista retornado com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Motorista não encontrado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType<ApiResponse<MotoristaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var motorista = await dispatcher.SendAsync(new GetMotoristaByIdQuery(id));

            if (motorista is null)
                return NotFound(ApiResponse<object>.Fail($"Motorista {id} não encontrado."));

            return Ok(ApiResponse<MotoristaResponse>.Ok(motorista));
        }
    }
}
