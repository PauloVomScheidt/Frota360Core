using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IPostoRepository
    {
        Task<IEnumerable<Posto>> GetAllAsync(int empresaId, bool apenasAtivos = false);
        Task<Posto?> GetByIdAsync(int id, int empresaId);
        Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null);
        Task<Posto> AddAsync(Posto posto);
        Task<Posto> UpdateAsync(Posto posto);
        Task DeleteAsync(Posto posto);
    }
}
