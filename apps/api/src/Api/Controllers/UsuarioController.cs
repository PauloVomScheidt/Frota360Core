using Asp.Versioning;
using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    // O Admin é a régua da gestão de usuários, mas /perfil é autoatendimento e vale para
    // todas as roles — atributos de classe e de ação combinam por E, então a restrição
    // administrativa fica em cada ação, e não aqui.
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UsuarioController(IUsuarioService usuarioService,
                                   IValidator<AlterarRoleRequest> alterarRoleValidator,
                                   IValidator<AtualizarPerfilRequest> atualizarPerfilValidator) : ControllerBase
    {
        /// <summary>Lista os usuários da empresa. (Admin)</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<IEnumerable<UsuarioResponse>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Listar()
        {
            var usuarios = await usuarioService.ListarAsync();
            return Ok(ApiResponse<IEnumerable<UsuarioResponse>>.Ok(usuarios));
        }

        /// <summary>Altera a role de um usuário da empresa; revoga a sessão dele. (Admin)</summary>
        /// <response code="200">Role alterada com sucesso</response>
        /// <response code="404">Usuário não encontrado</response>
        /// <response code="422">Regra do último administrador violada</response>
        [HttpPut("{id:int}/role")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<UsuarioResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> AlterarRole(int id, [FromBody] AlterarRoleRequest request)
        {
            var validation = await alterarRoleValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var usuario = await usuarioService.AlterarRoleAsync(id, request.Role);

            if (usuario is null)
                return NotFound(ApiResponse<object>.Fail($"Usuário {id} não encontrado."));

            return Ok(ApiResponse<UsuarioResponse>.Ok(usuario, "Role alterada com sucesso."));
        }

        /// <summary>Ativa ou desativa um usuário da empresa; desativar revoga a sessão. (Admin)</summary>
        /// <response code="200">Status alterado com sucesso</response>
        /// <response code="404">Usuário não encontrado</response>
        /// <response code="422">Regra do último administrador violada</response>
        [HttpPut("{id:int}/ativo")]
        [Authorize(Roles = Roles.Admin)]
        [ProducesResponseType<ApiResponse<UsuarioResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> DefinirAtivo(int id, [FromBody] DefinirAtivoRequest request)
        {
            var usuario = await usuarioService.DefinirAtivoAsync(id, request.Ativo);

            if (usuario is null)
                return NotFound(ApiResponse<object>.Fail($"Usuário {id} não encontrado."));

            var mensagem = request.Ativo ? "Usuário reativado com sucesso." : "Usuário desativado com sucesso.";
            return Ok(ApiResponse<UsuarioResponse>.Ok(usuario, mensagem));
        }

        /// <summary>Retorna o cadastro do próprio usuário logado. (Qualquer autenticado)</summary>
        /// <response code="200">Perfil retornado com sucesso</response>
        /// <response code="404">Usuário do token não existe mais</response>
        [HttpGet("perfil")]
        [ProducesResponseType<ApiResponse<UsuarioResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPerfil()
        {
            var usuario = await usuarioService.ObterPerfilAsync();

            if (usuario is null)
                return NotFound(ApiResponse<object>.Fail("Usuário não encontrado."));

            return Ok(ApiResponse<UsuarioResponse>.Ok(usuario));
        }

        /// <summary>
        /// Atualiza nome, CPF e data de nascimento do próprio usuário logado — o direito de
        /// correção da LGPD (Art. 18, III). O alvo vem do token; o e-mail não é editável aqui.
        /// (Qualquer autenticado)
        /// </summary>
        /// <response code="200">Perfil atualizado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="404">Usuário do token não existe mais</response>
        /// <response code="422">CPF já usado por outro usuário da empresa</response>
        [HttpPut("perfil")]
        [ProducesResponseType<ApiResponse<UsuarioResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> AtualizarPerfil([FromBody] AtualizarPerfilRequest request)
        {
            var validation = await atualizarPerfilValidator.ValidateAsync(request);

            if (!validation.IsValid)
            {
                var erros = validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BadRequest(ApiResponse<object>.Fail("Dados inválidos.", erros));
            }

            var usuario = await usuarioService.AtualizarPerfilAsync(request);

            if (usuario is null)
                return NotFound(ApiResponse<object>.Fail("Usuário não encontrado."));

            return Ok(ApiResponse<UsuarioResponse>.Ok(usuario, "Perfil atualizado com sucesso."));
        }
    }
}
