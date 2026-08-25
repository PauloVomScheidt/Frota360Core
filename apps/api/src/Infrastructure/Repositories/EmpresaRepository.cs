using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class EmpresaRepository(Frota360DbContext context) : IEmpresaRepository
    {
        public async Task<Empresa> AddAsync(Empresa empresa)
        {
            context.Empresas.Add(empresa);
            await context.SaveChangesAsync();
            return empresa;
        }

        public async Task<bool> ExisteCnpjAsync(string cnpj)
            => await context.Empresas.AnyAsync(e => e.CNPJ == cnpj);
    }
}
