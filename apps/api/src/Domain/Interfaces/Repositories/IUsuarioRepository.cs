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
        Task<Usuario> AddAsync(Usuario usuario);
        Task<Usuario> UpdateAsync(Usuario usuario);
    }
}
