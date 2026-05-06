using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class MotoristaRepository(Frota360DbContext context) : IMotoristaRepository
    {
        public async Task<IEnumerable<Motorista>> GetAllAsync()
            => await context.Motoristas.AsNoTracking().ToListAsync();

        public async Task<Motorista?> GetByIdAsync(int id)
            => await context.Motoristas.FirstOrDefaultAsync(v => v.Id == id);

        public async Task<Motorista> AddAsync(Motorista motorista)
        {
            context.Motoristas.Add(motorista);
            await context.SaveChangesAsync();
            return motorista;
        }

        public async Task<Motorista> UpdateAsync(Motorista motorista)
        {
            context.Motoristas.Update(motorista);
            await context.SaveChangesAsync();
            return motorista;
        }

        public async Task DeleteAsync(Motorista motorista)
        {
            context.Motoristas.Remove(motorista);
            await context.SaveChangesAsync();
        }
    }
}
