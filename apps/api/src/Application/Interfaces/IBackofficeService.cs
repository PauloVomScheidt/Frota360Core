using Frota360.Application.DTOs.Backoffice.Request;
using Frota360.Application.DTOs.Backoffice.Response;

namespace Frota360.Application.Interfaces
{
    public interface IBackofficeService
    {
        /// <summary>Provisiona uma empresa nova (venda assistida) e gera o convite do primeiro Admin.</summary>
        Task<EmpresaProvisionadaResponse> ProvisionarEmpresaAsync(ProvisionarEmpresaRequest request);
    }
}
