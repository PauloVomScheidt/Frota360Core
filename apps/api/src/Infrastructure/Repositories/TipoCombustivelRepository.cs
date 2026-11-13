using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class TipoCombustivelRepository(Frota360DbContext context) : ITipoCombustivelRepository
    {
        public async Task<IEnumerable<TipoCombustivel>> GetAllAsync(int empresaId, bool apenasAtivos = false)
            => await context.TiposCombustivel.AsNoTracking()
                .Where(t => t.EmpresaId == empresaId && (!apenasAtivos || t.Ativo))
                .OrderBy(t => t.Nome)
                .ToListAsync();

        public async Task<TipoCombustivel?> GetByIdAsync(int id, int empresaId)
            => await context.TiposCombustivel.FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        public async Task<bool> ExisteNomeAsync(int empresaId, string nome, int? ignorarId = null)
            => await context.TiposCombustivel.AsNoTracking()
                .AnyAsync(t => t.EmpresaId == empresaId
                            && t.Nome == nome
                            && (ignorarId == null || t.Id != ignorarId));

        public async Task<TipoCombustivel> AddAsync(TipoCombustivel tipo)
        {
            context.TiposCombustivel.Add(tipo);
            await context.SaveChangesAsync();
            return tipo;
        }

        public async Task AddRangeAsync(IEnumerable<TipoCombustivel> tipos)
        {
            context.TiposCombustivel.AddRange(tipos);
            await context.SaveChangesAsync();
        }

        public async Task<TipoCombustivel> UpdateAsync(TipoCombustivel tipo)
        {
            context.TiposCombustivel.Update(tipo);
            await context.SaveChangesAsync();
            return tipo;
        }

        public async Task DeleteAsync(TipoCombustivel tipo)
        {
            context.TiposCombustivel.Remove(tipo);
            await context.SaveChangesAsync();
        }
    }
}
