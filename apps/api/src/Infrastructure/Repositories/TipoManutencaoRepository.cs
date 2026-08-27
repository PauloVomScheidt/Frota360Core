using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class TipoManutencaoRepository(Frota360DbContext context) : ITipoManutencaoRepository
    {
        public async Task<IEnumerable<TipoManutencao>> GetAllAsync(int empresaId, bool apenasAtivos = false)
            => await context.TiposManutencao.AsNoTracking()
                .Where(t => t.EmpresaId == empresaId && (!apenasAtivos || t.Ativo))
                .OrderBy(t => t.Nome)
                .ToListAsync();

        public async Task<TipoManutencao?> GetByIdAsync(int id, int empresaId)
            => await context.TiposManutencao.FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        public async Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null)
            => await context.TiposManutencao.AsNoTracking()
                .AnyAsync(t => t.EmpresaId == empresaId
                            && t.Nome == nome
                            && (ignorarId == null || t.Id != ignorarId));

        public async Task<TipoManutencao> AddAsync(TipoManutencao tipo)
        {
            context.TiposManutencao.Add(tipo);
            await context.SaveChangesAsync();
            return tipo;
        }

        public async Task AddRangeAsync(IEnumerable<TipoManutencao> tipos)
        {
            context.TiposManutencao.AddRange(tipos);
            await context.SaveChangesAsync();
        }

        public async Task<TipoManutencao> UpdateAsync(TipoManutencao tipo)
        {
            context.TiposManutencao.Update(tipo);
            await context.SaveChangesAsync();
            return tipo;
        }

        public async Task DeleteAsync(TipoManutencao tipo)
        {
            context.TiposManutencao.Remove(tipo);
            await context.SaveChangesAsync();
        }
    }
}
