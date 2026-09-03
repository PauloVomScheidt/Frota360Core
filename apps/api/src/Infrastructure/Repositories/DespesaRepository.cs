using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class DespesaRepository(Frota360DbContext context) : IDespesaRepository
    {
        public async Task<IEnumerable<Despesa>> GetAllAsync(int empresaId, int? veiculoId = null,
            int? motoristaId = null, int? tipoDespesaId = null, DateTime? de = null, DateTime? ate = null)
        {
            var consulta = ComIncludes().Where(d => d.EmpresaId == empresaId);

            if (veiculoId is not null)
                consulta = consulta.Where(d => d.VeiculoId == veiculoId);

            if (motoristaId is not null)
                consulta = consulta.Where(d => d.MotoristaId == motoristaId);

            if (tipoDespesaId is not null)
                consulta = consulta.Where(d => d.TipoDespesaId == tipoDespesaId);

            if (de is not null)
            {
                var inicio = de.Value.Date;
                consulta = consulta.Where(d => d.DataDespesa >= inicio);
            }

            // "Até" é inclusivo: quem escolhe 11/08 espera ver o que lançou às 23h.
            if (ate is not null)
            {
                var fim = ate.Value.Date.AddDays(1);
                consulta = consulta.Where(d => d.DataDespesa < fim);
            }

            return await consulta
                .OrderByDescending(d => d.DataDespesa)
                .ThenByDescending(d => d.Id) // desempate estável no mesmo dia
                .ToListAsync();
        }

        public async Task<Despesa?> GetByIdAsync(int id, int empresaId)
            => await ComIncludes().FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);

        public async Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId)
            => await context.Despesas.AsNoTracking()
                .AnyAsync(d => d.EmpresaId == empresaId && d.VeiculoId == veiculoId);

        public async Task<bool> ExisteComTipoAsync(int empresaId, int tipoDespesaId)
            => await context.Despesas.AsNoTracking()
                .AnyAsync(d => d.EmpresaId == empresaId && d.TipoDespesaId == tipoDespesaId);

        public async Task<Despesa> AddAsync(Despesa despesa)
        {
            context.Despesas.Add(despesa);
            await context.SaveChangesAsync();
            return despesa;
        }

        public async Task<Despesa> UpdateAsync(Despesa despesa)
        {
            context.Despesas.Update(despesa);
            await context.SaveChangesAsync();
            return despesa;
        }

        public async Task DeleteAsync(Despesa despesa)
        {
            context.Despesas.Remove(despesa);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Rastreado de propósito: o mesmo carregamento serve para leitura, correção e
        /// exclusão — como em <c>AbastecimentoRepository.ComIncludes</c>.
        /// </summary>
        private IQueryable<Despesa> ComIncludes()
            => context.Despesas
                .Include(d => d.Veiculo)
                .Include(d => d.Tipo)
                .Include(d => d.Motorista);
    }
}
