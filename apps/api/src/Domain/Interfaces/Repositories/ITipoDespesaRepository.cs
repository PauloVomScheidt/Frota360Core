using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface ITipoDespesaRepository
    {
        Task<IEnumerable<TipoDespesa>> GetAllAsync(int empresaId, bool apenasAtivos = false);
        Task<TipoDespesa?> GetByIdAsync(int id, int empresaId);
        Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null);
        Task<TipoDespesa> AddAsync(TipoDespesa tipo);

        /// <summary>Semeadura do catálogo padrão no provisionamento da empresa.</summary>
        Task AddRangeAsync(IEnumerable<TipoDespesa> tipos);

        Task<TipoDespesa> UpdateAsync(TipoDespesa tipo);
        Task DeleteAsync(TipoDespesa tipo);
    }
}
