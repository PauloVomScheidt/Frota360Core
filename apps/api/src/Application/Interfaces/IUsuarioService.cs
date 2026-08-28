using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Application.DTOs.Usuario.Response;

namespace Frota360.Application.Interfaces
{
    public interface IUsuarioService
    {
        Task<IEnumerable<UsuarioResponse>> ListarAsync();

        /// <summary>
        /// Cadastro do próprio usuário logado. Existe porque <c>ListarAsync</c> é Admin-only:
        /// sem isto um Motorista não teria como ler os próprios dados pessoais.
        /// </summary>
        Task<UsuarioResponse?> ObterPerfilAsync();

        /// <summary>
        /// Edição do próprio cadastro (nome, CPF e nascimento) — o caminho do direito de
        /// correção da LGPD. Lança se o CPF colidir com o de outro usuário da empresa.
        /// </summary>
        Task<UsuarioResponse?> AtualizarPerfilAsync(AtualizarPerfilRequest request);

        /// <summary>Altera a role de um usuário da empresa. Null se não encontrado; lança se violar a regra do último admin.</summary>
        Task<UsuarioResponse?> AlterarRoleAsync(int usuarioId, string novaRole);

        /// <summary>Ativa/desativa um usuário da empresa. Null se não encontrado; lança se desativar o último admin ativo.</summary>
        Task<UsuarioResponse?> DefinirAtivoAsync(int usuarioId, bool ativo);
    }
}
