using Asp.Versioning;
using FluentValidation;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.UseCases.Rotas.Commands.CreateRota;
using Frota360.Application.UseCases.Rotas.Commands.DeleteRota;
using Frota360.Application.UseCases.Rotas.Commands.EncerrarRota;
using Frota360.Application.UseCases.Rotas.Commands.UpdateRota;
using Frota360.Application.UseCases.Rotas.Queries.GetAllRotas;
using Frota360.Application.UseCases.Rotas.Queries.GetMinhasRotas;
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
                                IValidator<UpdateRotaRequest> updateValidator,
                                IValidator<EncerrarRotaRequest> encerrarValidator) : ControllerBase
    {
        /// <summary>Retorna todas as rotas da empresa. (Admin, Supervisor, Operador)</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        /// <response code="403">Sem permissão — o motorista usa GET /rota/minhas</response>
        [HttpGet]
        [Authorize(Roles = Roles.Gestao)]
        [ProducesResponseType<ApiResponse<IEnumerable<RotaResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll()
        {
            var rotas = await dispatcher.SendAsync(new GetAllRotasQuery());
            return Ok(ApiResponse<IEnumerable<RotaResponse>>.Ok(rotas));
        }

        /// <summary>
        /// Rotas atribuídas ao motorista logado. (Motorista)
        /// Não recebe id de motorista: ele vem da claim, para que ninguém consiga pedir
        /// as rotas de outra pessoa.
        /// </summary>
        /// <response code="200">Lista retornada com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="422">Usuário sem vínculo com um cadastro de motorista</response>
        [HttpGet("minhas")]
        [Authorize(Roles = Roles.Motorista)]
        [ProducesResponseType<ApiResponse<IEnumerable<RotaResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetMinhas()
        {
            var rotas = await dispatcher.SendAsync(new GetMinhasRotasQuery());
            return Ok(ApiResponse<IEnumerable<RotaResponse>>.Ok(rotas));
        }

        /// <summary>Retorna uma rota pelo id. (Admin, Supervisor, Operador)</summary>
        /// <response code="200">Rota retornada com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpGet("{id:int}")]
        [Authorize(Roles = Roles.Gestao)]
        [ProducesResponseType<ApiResponse<RotaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var rota = await dispatcher.SendAsync(new GetRotaByIdQuery(id));

            if (rota is null)
                return NotFound(ApiResponse<object>.Fail($"Rota {id} não encontrada."));

            return Ok(ApiResponse<RotaResponse>.Ok(rota));
        }

        /// <summary>
        /// Cadastra uma nova rota. Aberto a qualquer autenticado, motorista incluído —
        /// mas para ele o <c>codigoMotorista</c> do corpo é ignorado: o handler grava o
        /// da claim, então ele só consegue abrir rota para si mesmo.
        /// </summary>
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

        /// <summary>Atualiza os dados de uma rota. (Admin, Supervisor, Operador)</summary>
        /// <response code="200">Atualizado com sucesso</response>
        /// <response code="403">Sem permissão — o motorista abre e encerra, mas não edita</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Gestao)]
        [ProducesResponseType<ApiResponse<RotaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
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

        /// <summary>
        /// Encerra a rota: grava o hodômetro final, calcula a quilometragem percorrida e
        /// avança o odômetro do veículo quando o valor informado for maior que o atual.
        /// Aberto a qualquer autenticado, incluindo Operador, em simetria com POST/PUT de rota —
        /// é ele quem opera a rota no dia a dia, e o avanço do odômetro é consequência
        /// controlada da regra, não edição livre do veículo.
        /// O motorista também encerra, mas só as próprias rotas: para ele, rota de outro
        /// responde 404 (checagem no handler).
        /// </summary>
        /// <response code="200">Rota encerrada com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Rota não encontrada</response>
        /// <response code="422">Rota já encerrada, km final menor que o inicial ou data de fim anterior à de início</response>
        [HttpPost("{id:int}/encerrar")]
        [ProducesResponseType<ApiResponse<RotaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Encerrar(int id, [FromBody] EncerrarRotaRequest request)
        {
            var validation = await encerrarValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var encerrada = await dispatcher.SendAsync(new EncerrarRotaCommand(id, request));

            if (encerrada is null)
                return NotFound(ApiResponse<object>.Fail($"Rota {id} não encontrada."));

            return Ok(ApiResponse<RotaResponse>.Ok(encerrada, "Rota encerrada com sucesso."));
        }

        /// <summary>Remove uma rota. (Admin)</summary>
        /// <response code="200">Removido com sucesso</response>
        /// <response code="403">Sem permissão</response>
        /// <response code="404">Rota não encontrada</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
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
