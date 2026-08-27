using Frota360.Application.DTOs.Usuario.Response;

namespace Frota360.Application.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioResponse>> ListarAsync();

        /// <summary>Altera a role de um usuário da empresa. Null se não encontrado; lança se violar a regra do último admin.</summary>
        Task<UsuarioResponse?> AlterarRoleAsync(int usuarioId, string novaRole);

        /// <summary>Ativa/desativa um usuário da empresa. Null se não encontrado; lança se desativar o último admin ativo.</summary>
        Task<UsuarioResponse?> DefinirAtivoAsync(int usuarioId, bool ativo);
    }
}
