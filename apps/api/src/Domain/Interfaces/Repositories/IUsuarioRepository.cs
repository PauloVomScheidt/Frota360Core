using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetByIdAsync(int id);
        Task<IEnumerable<Usuario>> GetAllByEmpresaAsync(int empresaId);
        Task<int> ContarAdminsAtivosAsync(int empresaId);
        Task<Usuario?> GetByRefreshTokenHashAsync(string refreshTokenHash);
        Task<Usuario?> GetByResetSenhaTokenHashAsync(string resetSenhaTokenHash);
        Task<bool> ExisteEmailAsync(string email);

        /// <summary>
        /// Colisão de CPF dentro da empresa, ignorando o próprio usuário — é a checagem que
        /// antecipa a violação do índice único filtrado <c>(EmpresaId, CPF)</c> e a transforma
        /// em 422 com mensagem, em vez de 500.
        /// </summary>
        Task<bool> ExisteCpfNaEmpresaAsync(int empresaId, string cpf, int ignorarUsuarioId);

        /// <summary>
        /// Motoristas da empresa — os usuários com a role Motorista, ativos e inativos.
        /// É a fonte da tela /motoristas e do seletor de motorista da rota.
        /// </summary>
        Task<IEnumerable<Usuario>> GetMotoristasByEmpresaAsync(int empresaId);

        /// <summary>
        /// Um motorista da empresa pelo id. Devolve null se o usuário não existe, é de
        /// outra empresa ou não tem a role Motorista — os três casos "não existe" para
        /// quem resolve o CodigoMotorista de uma rota.
        /// </summary>
        Task<Usuario?> GetMotoristaByIdAsync(int id, int empresaId);
        Task<Usuario> AddAsync(Usuario usuario);
        Task<Usuario> UpdateAsync(Usuario usuario);
    }
}
