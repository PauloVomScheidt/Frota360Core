using Frota360.Application.Common;
using Frota360.Application.DTOs.Usuario.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.Services
{
    public class UsuarioService(IUsuarioRepository repository,
                                ICurrentUserService currentUser,
                                IAuditoriaService auditoria,
                                ILogger<UsuarioService> logger) : IUsuarioService
    {
        public async Task<IEnumerable<UsuarioResponse>> ListarAsync()
        {
            var usuarios = await repository.GetAllByEmpresaAsync(currentUser.EmpresaId);
            return usuarios.Select(ToResponse);
        }

        public async Task<UsuarioResponse?> AlterarRoleAsync(int usuarioId, string novaRole)
        {
            var usuario = await ObterDaEmpresaAsync(usuarioId);

            if (usuario is null)
                return null;

            if (usuario.Role == novaRole)
                return ToResponse(usuario);

            if (usuario.Role == Roles.Admin && usuario.Ativo && await SeriaUltimoAdminAtivoAsync())
                throw new InvalidOperationException("Não é possível alterar a role do único administrador ativo da empresa.");

            var roleAnterior = usuario.Role;

            usuario.Role = novaRole;
            RevogarSessao(usuario); // força novo login para o token refletir a role nova

            await repository.UpdateAsync(usuario);

            logger.LogInformation("Role do usuário {Id} alterada para {Role}", usuarioId, novaRole);

            // Mudança de permissão é o evento mais consequente da trilha: é o que amplia
            // ou reduz o que alguém consegue fazer no sistema inteiro.
            await auditoria.RegistrarAsync(EntidadesAuditadas.Usuario, AcoesAuditoria.AlterouPermissao, usuario.Id,
                $"Alterou a permissão de {usuario.Nome} ({usuario.Email}) de {roleAnterior} para {novaRole}",
                new AlteracoesBuilder().Comparar("Permissão", roleAnterior, novaRole).Construir());

            return ToResponse(usuario);
        }

        public async Task<UsuarioResponse?> DefinirAtivoAsync(int usuarioId, bool ativo)
        {
            var usuario = await ObterDaEmpresaAsync(usuarioId);

            if (usuario is null)
                return null;

            if (usuario.Ativo == ativo)
                return ToResponse(usuario);

            if (!ativo && usuario.Role == Roles.Admin && await SeriaUltimoAdminAtivoAsync())
                throw new InvalidOperationException("Não é possível desativar o único administrador ativo da empresa.");

            usuario.Ativo = ativo;

            if (!ativo)
                RevogarSessao(usuario);

            await repository.UpdateAsync(usuario);

            logger.LogInformation("Usuário {Id} {Acao}", usuarioId, ativo ? "reativado" : "desativado");

            await auditoria.RegistrarAsync(EntidadesAuditadas.Usuario,
                ativo ? AcoesAuditoria.Ativou : AcoesAuditoria.Desativou, usuario.Id,
                $"{(ativo ? "Reativou" : "Desativou")} o usuário {usuario.Nome} ({usuario.Email})");

            return ToResponse(usuario);
        }

        private async Task<Usuario?> ObterDaEmpresaAsync(int usuarioId)
        {
            var usuario = await repository.GetByIdAsync(usuarioId);
            return usuario is null || usuario.EmpresaId != currentUser.EmpresaId ? null : usuario;
        }

        private async Task<bool> SeriaUltimoAdminAtivoAsync()
            => await repository.ContarAdminsAtivosAsync(currentUser.EmpresaId) <= 1;

        private static void RevogarSessao(Usuario usuario)
        {
            usuario.RefreshTokenHash = null;
            usuario.RefreshTokenExpiraEm = null;
        }

        private static UsuarioResponse ToResponse(Usuario u) => new()
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            Role = u.Role,
            CPF = u.CPF,
            DataNascimento = u.DataNascimento,
            Ativo = u.Ativo,
            DataInclusao = u.DataInclusao
        };
    }
}
