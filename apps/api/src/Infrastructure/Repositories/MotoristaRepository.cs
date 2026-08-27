using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class MotoristaRepository(Frota360DbContext context) : IMotoristaRepository
    {
        public async Task<IEnumerable<Motorista>> GetAllAsync(int empresaId)
            => await context.Motoristas.AsNoTracking()
                .Where(m => m.EmpresaId == empresaId)
                .ToListAsync();

        public async Task<Motorista?> GetByIdAsync(int id, int empresaId)
            => await context.Motoristas.FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == empresaId);

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
