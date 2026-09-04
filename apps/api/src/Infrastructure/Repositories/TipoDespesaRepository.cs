using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class TipoDespesaRepository(Frota360DbContext context) : ITipoDespesaRepository
    {
        public async Task<IEnumerable<TipoDespesa>> GetAllAsync(int empresaId, bool apenasAtivos = false)
            => await context.TiposDespesa.AsNoTracking()
                .Where(t => t.EmpresaId == empresaId && (!apenasAtivos || t.Ativo))
                .OrderBy(t => t.Nome)
                .ToListAsync();

        public async Task<TipoDespesa?> GetByIdAsync(int id, int empresaId)
            => await context.TiposDespesa.FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        public async Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null)
            => await context.TiposDespesa.AsNoTracking()
                .AnyAsync(t => t.EmpresaId == empresaId
                            && t.Nome == nome
                            && (ignorarId == null || t.Id != ignorarId));

        public async Task<TipoDespesa> AddAsync(TipoDespesa tipo)
        {
            context.TiposDespesa.Add(tipo);
            await context.SaveChangesAsync();
            return tipo;
        }

        public async Task AddRangeAsync(IEnumerable<TipoDespesa> tipos)
        {
            context.TiposDespesa.AddRange(tipos);
            await context.SaveChangesAsync();
        }

        public async Task<TipoDespesa> UpdateAsync(TipoDespesa tipo)
        {
            context.TiposDespesa.Update(tipo);
            await context.SaveChangesAsync();
            return tipo;
        }

        public async Task DeleteAsync(TipoDespesa tipo)
        {
            context.TiposDespesa.Remove(tipo);
            await context.SaveChangesAsync();
        }
    }
}
