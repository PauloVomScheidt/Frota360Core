using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class DespesaRepository(Frota360DbContext context) : IDespesaRepository
    {
        public async Task<(IEnumerable<Despesa> Itens, int Total)> ConsultarAsync(
            int empresaId, FiltroDespesa filtro)
        {
            var consulta = Filtrar(ComIncludes(), empresaId, filtro);

            var total = await consulta.CountAsync();

            var itens = await consulta
                .OrderByDescending(d => d.DataDespesa)
                .ThenByDescending(d => d.Id) // desempate estável: sem ele a página 2 repete linhas
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .ToListAsync();

            return (itens, total);
        }

        public async Task<ResumoLancamentos> ResumirAsync(int empresaId, FiltroDespesa filtro)
        {
            // Sem os Includes: agregação não precisa das navegações.
            var consulta = Filtrar(context.Despesas.AsNoTracking(), empresaId, filtro);

            var resumo = await consulta
                .GroupBy(_ => 1)
                .Select(g => new { Quantidade = g.Count(), ValorTotal = g.Sum(d => d.Valor) })
                .FirstOrDefaultAsync();

            return new ResumoLancamentos(resumo?.Quantidade ?? 0, resumo?.ValorTotal ?? 0m);
        }

        /// <summary>
        /// O `Where` compartilhado pela página e pelo resumo. Os dois <b>precisam</b> enxergar
        /// exatamente o mesmo recorte: se divergirem, o rodapé passa a somar um conjunto que a
        /// tabela não mostra.
        /// </summary>
        private static IQueryable<Despesa> Filtrar(IQueryable<Despesa> consulta, int empresaId,
            FiltroDespesa filtro)
        {
            consulta = consulta.Where(d => d.EmpresaId == empresaId);

            if (filtro.VeiculoId is not null)
                consulta = consulta.Where(d => d.VeiculoId == filtro.VeiculoId);

            if (filtro.MotoristaId is not null)
                consulta = consulta.Where(d => d.MotoristaId == filtro.MotoristaId);

            if (filtro.TipoDespesaId is not null)
                consulta = consulta.Where(d => d.TipoDespesaId == filtro.TipoDespesaId);

            if (filtro.De is not null)
            {
                var inicio = filtro.De.Value.Date;
                consulta = consulta.Where(d => d.DataDespesa >= inicio);
            }

            // "Até" é inclusivo: quem escolhe 11/08 espera ver o que lançou às 23h.
            if (filtro.Ate is not null)
            {
                var fim = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(d => d.DataDespesa < fim);
            }

            return consulta;
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
