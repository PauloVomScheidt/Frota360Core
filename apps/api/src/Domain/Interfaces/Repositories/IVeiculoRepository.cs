using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IVeiculoRepository
    {
        Task<IEnumerable<Veiculo>> GetAllAsync(int empresaId);
        Task<Veiculo> AddAsync(Veiculo veiculo);
        Task<Veiculo?> GetByIdAsync(int id, int empresaId);
        Task<Veiculo> UpdateAsync(Veiculo veiculo);
        Task DeleteAsync(Veiculo veiculo); 
    }
}
