using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.DTOs.Motorista.Response;

namespace Frota360.Application.Interfaces
{
    public interface IMotoristaService
    {
        Task<IEnumerable<MotoristaResponse>> GetAllAsync();
        Task<MotoristaResponse> AddAsync(CreateMotoristaRequest request);
        Task<MotoristaResponse?> UpdateAsync(int id, UpdateMotoristaRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
