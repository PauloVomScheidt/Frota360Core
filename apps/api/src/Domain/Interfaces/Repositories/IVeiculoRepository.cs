using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IVeiculoRepository
    {
        Task<IEnumerable<Veiculo>> GetAllAsync();
        Task<Veiculo> AddAsync(Veiculo veiculo);
        Task<Veiculo?> GetByIdAsync(int id);  
        Task<Veiculo> UpdateAsync(Veiculo veiculo);
        Task DeleteAsync(Veiculo veiculo); 
    }
}
