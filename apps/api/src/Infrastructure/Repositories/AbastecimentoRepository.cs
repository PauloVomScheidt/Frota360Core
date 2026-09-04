using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class AbastecimentoRepository(Frota360DbContext context) : IAbastecimentoRepository
    {
        public async Task<IEnumerable<Abastecimento>> GetAllAsync(int empresaId, int? veiculoId = null,
            int? motoristaId = null, DateTime? de = null, DateTime? ate = null)
        {
            var consulta = ComIncludes().Where(a => a.EmpresaId == empresaId);

            if (veiculoId is not null)
                consulta = consulta.Where(a => a.VeiculoId == veiculoId);

            if (motoristaId is not null)
                consulta = consulta.Where(a => a.MotoristaId == motoristaId);

            if (de is not null)
            {
                var inicio = de.Value.Date;
                consulta = consulta.Where(a => a.DataAbastecimento >= inicio);
            }

            // "Até" é inclusivo: quem escolhe 11/08 espera ver o que abasteceu às 23h.
            if (ate is not null)
            {
                var fim = ate.Value.Date.AddDays(1);
                consulta = consulta.Where(a => a.DataAbastecimento < fim);
            }

            return await consulta
                .OrderByDescending(a => a.DataAbastecimento)
                .ThenByDescending(a => a.Id) // desempate estável no mesmo dia
                .ToListAsync();
        }

        public async Task<Abastecimento?> GetByIdAsync(int id, int empresaId)
            => await ComIncludes().FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);

        public async Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId)
            => await context.Abastecimentos.AsNoTracking()
                .AnyAsync(a => a.EmpresaId == empresaId && a.VeiculoId == veiculoId);

        public async Task<bool> ExisteComTipoCombustivelAsync(int empresaId, int tipoCombustivelId)
            => await context.Abastecimentos.AsNoTracking()
                .AnyAsync(a => a.EmpresaId == empresaId && a.TipoCombustivelId == tipoCombustivelId);

        public async Task<bool> ExisteComPostoAsync(int empresaId, int postoId)
            => await context.Abastecimentos.AsNoTracking()
                .AnyAsync(a => a.EmpresaId == empresaId && a.PostoId == postoId);

        public async Task<Abastecimento> AddAsync(Abastecimento abastecimento)
        {
            context.Abastecimentos.Add(abastecimento);
            await context.SaveChangesAsync();
            return abastecimento;
        }

        public async Task<Abastecimento> UpdateAsync(Abastecimento abastecimento)
        {
            context.Abastecimentos.Update(abastecimento);
            await context.SaveChangesAsync();
            return abastecimento;
        }

        public async Task DeleteAsync(Abastecimento abastecimento)
        {
            context.Abastecimentos.Remove(abastecimento);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Rastreado de propósito: o mesmo carregamento serve para leitura, correção e
        /// exclusão — como em <c>ManutencaoRepository.GetByIdAsync</c>.
        /// </summary>
        private IQueryable<Abastecimento> ComIncludes()
            => context.Abastecimentos
                .Include(a => a.Veiculo)
                .Include(a => a.Rota)
                .Include(a => a.Motorista)
                .Include(a => a.Usuario)
                .Include(a => a.TipoCombustivel)
                .Include(a => a.Posto);
    }
}
