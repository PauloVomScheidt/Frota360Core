using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class ConviteRepository(Frota360DbContext context) : IConviteRepository
    {
        public async Task<IEnumerable<Convite>> GetAllAsync(int empresaId)
            => await context.Convites.AsNoTracking()
                .Where(c => c.EmpresaId == empresaId)
                .OrderByDescending(c => c.DataInclusao)
                .ToListAsync();

        public async Task<Convite?> GetByIdAsync(int id, int empresaId)
            => await context.Convites.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

        public async Task<Convite?> GetByTokenHashAsync(string tokenHash)
            => await context.Convites.FirstOrDefaultAsync(c => c.TokenHash == tokenHash);

        public async Task<IEnumerable<Convite>> GetPendentesByEmailAsync(string email, int empresaId)
            => await context.Convites
                .Where(c => c.Email == email && c.EmpresaId == empresaId && c.UtilizadoEm == null)
                .ToListAsync();

        public async Task<Convite> AddAsync(Convite convite)
        {
            context.Convites.Add(convite);
            await context.SaveChangesAsync();
            return convite;
        }

        public async Task<Convite> UpdateAsync(Convite convite)
        {
            context.Convites.Update(convite);
            await context.SaveChangesAsync();
            return convite;
        }

        public async Task DeleteAsync(Convite convite)
        {
            context.Convites.Remove(convite);
            await context.SaveChangesAsync();
        }
    }
}
