using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class AbastecimentoRepository(Frota360DbContext context) : IAbastecimentoRepository
    {
        public async Task<(IEnumerable<Abastecimento> Itens, int Total)> ConsultarAsync(
            int empresaId, FiltroAbastecimento filtro)
        {
            var consulta = Filtrar(ComIncludes(), empresaId, filtro);

            var total = await consulta.CountAsync();

            var itens = await consulta
                .OrderByDescending(a => a.DataAbastecimento)
                .ThenByDescending(a => a.Id) // desempate estável: sem ele a página 2 repete linhas
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .ToListAsync();

            return (itens, total);
        }

        public async Task<ResumoLancamentos> ResumirAsync(int empresaId, FiltroAbastecimento filtro)
        {
            // Sem os Includes: agregação não precisa das navegações, e trazê-las faria o EF
            // montar um join que o COUNT/SUM não usa.
            var consulta = Filtrar(context.Abastecimentos.AsNoTracking(), empresaId, filtro);

            // Uma consulta só para os dois números. `SumAsync` sobre conjunto vazio devolveria
            // zero de qualquer forma, mas o GroupBy explicita que é uma ida ao banco, não duas.
            var resumo = await consulta
                .GroupBy(_ => 1)
                .Select(g => new { Quantidade = g.Count(), ValorTotal = g.Sum(a => a.Valor) })
                .FirstOrDefaultAsync();

            return new ResumoLancamentos(resumo?.Quantidade ?? 0, resumo?.ValorTotal ?? 0m);
        }

        public async Task<Abastecimento?> GetAnteriorPorOdometroAsync(int empresaId, int veiculoId,
            int odometro, int? ignorarId = null)
            => await context.Abastecimentos.AsNoTracking()
                .Where(a => a.EmpresaId == empresaId && a.VeiculoId == veiculoId && a.Odometro < odometro)
                .Where(a => ignorarId == null || a.Id != ignorarId)
                // Por odômetro, não por data: é o que impede um lançamento retroativo de
                // virar quilometragem negativa na estimativa.
                .OrderByDescending(a => a.Odometro)
                .FirstOrDefaultAsync();

        /// <summary>
        /// O `Where` compartilhado pela página e pelo resumo. Os dois <b>precisam</b> enxergar
        /// exatamente o mesmo recorte: se divergirem, o rodapé passa a somar um conjunto que a
        /// tabela não mostra.
        /// </summary>
        private static IQueryable<Abastecimento> Filtrar(IQueryable<Abastecimento> consulta,
            int empresaId, FiltroAbastecimento filtro)
        {
            consulta = consulta.Where(a => a.EmpresaId == empresaId);

            if (filtro.VeiculoId is not null)
                consulta = consulta.Where(a => a.VeiculoId == filtro.VeiculoId);

            if (filtro.MotoristaId is not null)
                consulta = consulta.Where(a => a.MotoristaId == filtro.MotoristaId);

            if (filtro.De is not null)
            {
                var inicio = filtro.De.Value.Date;
                consulta = consulta.Where(a => a.DataAbastecimento >= inicio);
            }

            // "Até" é inclusivo: quem escolhe 11/08 espera ver o que abasteceu às 23h.
            if (filtro.Ate is not null)
            {
                var fim = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(a => a.DataAbastecimento < fim);
            }

            return consulta;
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
