using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class PostoRepository(Frota360DbContext context) : IPostoRepository
    {
        public async Task<IEnumerable<Posto>> GetAllAsync(int empresaId, bool apenasAtivos = false)
            => await context.Postos.AsNoTracking()
                .Where(p => p.EmpresaId == empresaId && (!apenasAtivos || p.Ativo))
                .OrderBy(p => p.Nome)
                .ToListAsync();

        public async Task<Posto?> GetByIdAsync(int id, int empresaId)
            => await context.Postos.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);

        public async Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null)
            => await context.Postos.AsNoTracking()
                .AnyAsync(p => p.EmpresaId == empresaId
                            && p.Nome == nome
                            && (ignorarId == null || p.Id != ignorarId));

        public async Task<Posto> AddAsync(Posto posto)
        {
            context.Postos.Add(posto);
            await context.SaveChangesAsync();
            return posto;
        }

        public async Task<Posto> UpdateAsync(Posto posto)
        {
            context.Postos.Update(posto);
            await context.SaveChangesAsync();
            return posto;
        }

        public async Task DeleteAsync(Posto posto)
        {
            context.Postos.Remove(posto);
            await context.SaveChangesAsync();
        }
    }
}
