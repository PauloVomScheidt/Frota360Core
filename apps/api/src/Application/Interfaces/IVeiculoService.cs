using Frota360.Application.DTOs.Veiculo;

namespace Frota360.Application.Interfaces
{
    public interface IVeiculoService
    {
        Task<IEnumerable<VeiculoResponse>> GetAllAsync();
    }
}
