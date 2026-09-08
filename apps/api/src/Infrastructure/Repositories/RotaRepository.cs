using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class RotaRepository(Frota360DbContext context) : IRotaRepository
    {
        public Task<(IEnumerable<Rota> Itens, int Total)> ConsultarAsync(int empresaId, FiltroRota filtro)
            => PaginarAsync(ComIncludes().Where(r => r.EmpresaId == empresaId), filtro);

        public Task<(IEnumerable<Rota> Itens, int Total)> ConsultarDoMotoristaAsync(
            int empresaId, int motoristaId, FiltroRota filtro)
            => PaginarAsync(
                ComIncludes().Where(r => r.EmpresaId == empresaId && r.CodigoMotorista == motoristaId),
                filtro);

        public async Task<ResumoRotas> ResumirEncerradasAsync(int empresaId, DateTime de, DateTime ate)
        {
            // "Até" inclusivo, como nos demais filtros de período do sistema.
            var fim = ate.Date.AddDays(1);
            var inicio = de.Date;

            var resumo = await context.Rotas.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.KmPercorrido != null
                            && r.DataFim >= inicio && r.DataFim < fim)
                .GroupBy(_ => 1)
                .Select(g => new { Quantidade = g.Count(), KmTotal = g.Sum(r => r.KmPercorrido!.Value) })
                .FirstOrDefaultAsync();

            return new ResumoRotas(resumo?.Quantidade ?? 0, resumo?.KmTotal ?? 0);
        }

        public async Task<Rota?> GetRotaAbertaDoMotoristaAsync(int empresaId, int motoristaId)
            => await context.Rotas.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.CodigoMotorista == motoristaId
                            && r.Ativo && r.DataFim == null)
                .OrderByDescending(r => r.DataInicio)
                .ThenByDescending(r => r.Id)
                .FirstOrDefaultAsync();

        /// <summary>
        /// O filtro de estado e a paginação, comuns às duas consultas. `Ativo` é campo persistido
        /// (diferente de "rota aberta", que é `Ativo && DataFim == null` — ver
        /// <c>GetVeiculosEmRotaAsync</c> abaixo).
        /// </summary>
        private static async Task<(IEnumerable<Rota> Itens, int Total)> PaginarAsync(
            IQueryable<Rota> consulta, FiltroRota filtro)
        {
            if (filtro.Ativo is not null)
                consulta = consulta.Where(r => r.Ativo == filtro.Ativo);

            var total = await consulta.CountAsync();

            // Da mais recente para a mais antiga, com desempate por id: sem ele duas rotas
            // abertas no mesmo dia podem trocar de lugar e a página 2 repetir uma linha.
            var itens = await consulta
                .OrderByDescending(r => r.DataInicio)
                .ThenByDescending(r => r.Id)
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .ToListAsync();

            return (itens, total);
        }

        /// <summary>
        /// O veículo entra junto porque <c>RotaResponse</c> desnormaliza placa e nome — antes a
        /// tela montava um mapa a partir da lista inteira de veículos, o que a paginação
        /// inviabilizou.
        /// </summary>
        private IQueryable<Rota> ComIncludes()
            => context.Rotas.AsNoTracking()
                .Include(r => r.Motorista)
                .Include(r => r.Veiculo);

        public async Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId)
            => await context.Rotas.AsNoTracking()
                .AnyAsync(r => r.EmpresaId == empresaId && r.CodigoVeiculo == veiculoId);

        // "Aberta" é `Ativo && DataFim is null` — não há estado persistido, é esta derivação
        // que o app inteiro usa. Distinct porque nada impede duas rotas abertas no mesmo carro.
        public async Task<IReadOnlyCollection<int>> GetVeiculosEmRotaAsync(int empresaId)
            => await context.Rotas.AsNoTracking()
                .Where(r => r.EmpresaId == empresaId && r.Ativo && r.DataFim == null)
                .Select(r => r.CodigoVeiculo)
                .Distinct()
                .ToListAsync();

        public async Task<bool> ExisteRotaAtivaComVeiculoAsync(int empresaId, int veiculoId)
            => await context.Rotas.AsNoTracking()
                .AnyAsync(r => r.EmpresaId == empresaId && r.CodigoVeiculo == veiculoId
                               && r.Ativo && r.DataFim == null);

        // Rastreado (sem AsNoTracking): serve tanto para leitura quanto para update.
        public async Task<Rota?> GetByIdAsync(int id, int empresaId)
            => await context.Rotas
                .Include(r => r.Motorista)
                .FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId);

        public async Task<Rota> AddAsync(Rota rota)
        {
            context.Rotas.Add(rota);
            await context.SaveChangesAsync();
            return rota;
        }

        public async Task DeleteAsync(Rota rota)
        {
            context.Rotas.Remove(rota);
            await context.SaveChangesAsync();
        }

        public async Task<Rota> UpdateAsync(Rota rota)
        {
            context.Rotas.Update(rota);
            await context.SaveChangesAsync();
            return rota;
        }
    }
}
