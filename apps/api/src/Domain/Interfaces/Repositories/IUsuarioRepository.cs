using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<bool> ExisteEmailAsync(string email);
        Task<Usuario> AddAsync(Usuario usuario);
    }
}
