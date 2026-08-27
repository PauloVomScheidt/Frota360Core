using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IConviteRepository
    {
        Task<IEnumerable<Convite>> GetAllAsync(int empresaId);
        Task<Convite?> GetByIdAsync(int id, int empresaId);
        Task<Convite?> GetByTokenHashAsync(string tokenHash);
        Task<IEnumerable<Convite>> GetPendentesByEmailAsync(string email, int empresaId);
        Task<Convite> AddAsync(Convite convite);
        Task<Convite> UpdateAsync(Convite convite);
        Task DeleteAsync(Convite convite);
    }
}
