using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface ITipoManutencaoRepository
    {
        Task<IEnumerable<TipoManutencao>> GetAllAsync(int empresaId, bool apenasAtivos = false);
        Task<TipoManutencao?> GetByIdAsync(int id, int empresaId);
        Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null);
        Task<TipoManutencao> AddAsync(TipoManutencao tipo);
        Task AddRangeAsync(IEnumerable<TipoManutencao> tipos);
        Task<TipoManutencao> UpdateAsync(TipoManutencao tipo);
        Task DeleteAsync(TipoManutencao tipo);
    }
}
