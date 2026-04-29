using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.DTOs.Veiculo.Response;

namespace Frota360.Application.Interfaces
{
    public interface IVeiculoService
    {
        Task<IEnumerable<VeiculoResponse>> GetAllAsync();
        Task<VeiculoResponse> AddAsync(CreateVeiculoRequest request);
        Task<VeiculoResponse?> UpdateAsync(int id, UpdateVeiculoRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
