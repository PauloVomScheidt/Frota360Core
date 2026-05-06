using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class RotaRepository(Frota360DbContext context) : IRotaRepository
    {
        public async Task<IEnumerable<Rota>> GetAllAsync()
            => await context.Rotas.AsNoTracking().ToListAsync();

        public async Task<Rota?> GetByIdAsync(int id)
        => await context.Rotas.FirstOrDefaultAsync(v => v.Id == id);

        public async Task<Rota> AddAsync(Rota rota)
        {
            context.Rotas.Add(rota);
            await context.SaveChangesAsync();
            return rota;
        }

        public async Task DeleteAsync(Rota rota)
        {
            context.Rotas.Remove(rota);
            await context.SaveChangesAsync();
        }

        public async Task<Rota> UpdateAsync(Rota rota)
        {
            context.Rotas.Update(rota);
            await context.SaveChangesAsync();
            return rota;
        }
    }
}
