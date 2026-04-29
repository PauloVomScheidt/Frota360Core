using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class VeiculoRepository(Frota360DbContext context) : IVeiculoRepository
    {
        public async Task<IEnumerable<Veiculo>> GetAllAsync()
            => await context.Veiculos.AsNoTracking().ToListAsync();

        public async Task<Veiculo?> GetByIdAsync(int id)
        => await context.Veiculos.FirstOrDefaultAsync(v => v.Id == id);

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
