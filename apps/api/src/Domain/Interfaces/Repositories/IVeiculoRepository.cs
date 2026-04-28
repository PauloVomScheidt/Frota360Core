using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IVeiculoRepository
    {
        Task<IEnumerable<Veiculo>> GetAllAsync();
    }
}
