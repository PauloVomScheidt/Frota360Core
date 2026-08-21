using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IMotoristaRepository
    {
        Task<IEnumerable<Motorista>> GetAllAsync(int empresaId);
        Task<Motorista> AddAsync(Motorista motorista);
        Task<Motorista?> GetByIdAsync(int id, int empresaId);
        Task<Motorista> UpdateAsync(Motorista motorista);
        Task DeleteAsync(Motorista motorista);
    }
}
