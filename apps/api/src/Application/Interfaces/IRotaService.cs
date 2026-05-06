using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.Interfaces
{
    public interface IRotaService
    {
        Task<IEnumerable<RotaResponse>> GetAllAsync();
        Task<RotaResponse> AddAsync(CreateRotaRequest request);
        Task<RotaResponse?> UpdateAsync(int id, UpdateRotaRequest request);
        Task<bool> DeleteAsync(int id);
    }
}