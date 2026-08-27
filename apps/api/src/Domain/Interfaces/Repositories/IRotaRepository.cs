using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IRotaRepository
    {
        Task<IEnumerable<Rota>> GetAllAsync(int empresaId);

        /// <summary>Rotas de um único motorista — base da tela "Minhas rotas".</summary>
        Task<IEnumerable<Rota>> GetAllByMotoristaAsync(int empresaId, int motoristaId);

        Task<Rota> AddAsync(Rota rota);
        Task<Rota?> GetByIdAsync(int id, int empresaId);
        Task<Rota> UpdateAsync(Rota rota);
        Task DeleteAsync(Rota rota);
    }
}
