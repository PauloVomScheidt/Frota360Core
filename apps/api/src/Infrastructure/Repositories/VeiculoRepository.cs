using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class VeiculoRepository(Frota360DbContext context) : IVeiculoRepository
    {
        public async Task<IEnumerable<Veiculo>> GetAllAsync(int empresaId)
            => await context.Veiculos.AsNoTracking()
                .Where(v => v.EmpresaId == empresaId)
                .ToListAsync();

        public async Task<Veiculo?> GetByIdAsync(int id, int empresaId)
            => await context.Veiculos.FirstOrDefaultAsync(v => v.Id == id && v.EmpresaId == empresaId);

        public async Task<Veiculo> AddAsync(Veiculo veiculo)
        {
            context.Veiculos.Add(veiculo);
            await context.SaveChangesAsync();
            return veiculo;
        }

        public async Task DeleteAsync(Veiculo veiculo)
        {
            context.Veiculos.Remove(veiculo);
            await context.SaveChangesAsync();
        }

        public async Task<Veiculo> UpdateAsync(Veiculo veiculo)
        {
            context.Veiculos.Update(veiculo);
            await context.SaveChangesAsync();
            return veiculo;
        }
    }
}
