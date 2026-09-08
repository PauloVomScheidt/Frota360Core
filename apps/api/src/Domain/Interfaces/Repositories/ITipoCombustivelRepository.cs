using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface ITipoCombustivelRepository
    {
        Task<IEnumerable<TipoCombustivel>> GetAllAsync(int empresaId, bool apenasAtivos = false);
        Task<TipoCombustivel?> GetByIdAsync(int id, int empresaId);
        Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null);
        Task<TipoCombustivel> AddAsync(TipoCombustivel tipo);

        /// <summary>Semeadura do catálogo padrão no provisionamento da empresa.</summary>
        Task AddRangeAsync(IEnumerable<TipoCombustivel> tipos);

        Task<TipoCombustivel> UpdateAsync(TipoCombustivel tipo);
        Task DeleteAsync(TipoCombustivel tipo);
    }
}
