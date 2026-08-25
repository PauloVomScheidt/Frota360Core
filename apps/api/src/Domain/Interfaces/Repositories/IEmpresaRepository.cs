using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IEmpresaRepository
    {
        Task<Empresa> AddAsync(Empresa empresa);
        Task<bool> ExisteCnpjAsync(string cnpj);
    }
}
