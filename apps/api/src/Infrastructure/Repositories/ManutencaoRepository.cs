using Frota360.Domain.Entities;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class ManutencaoRepository(Frota360DbContext context) : IManutencaoRepository
    {
        public async Task<IEnumerable<Manutencao>> GetAllAsync(int empresaId, int? veiculoId = null, StatusManutencao? status = null)
            => await context.Manutencoes.AsNoTracking()
                .Include(m => m.Veiculo)
                .Include(m => m.Tipo)
                .Where(m => m.EmpresaId == empresaId
                         && (veiculoId == null || m.VeiculoId == veiculoId)
                         && (status == null || m.Status == status))
                // Status é persistido como texto, então OrderBy(Status) sairia em ordem
                // alfabética. O que a tela quer é o que ainda precisa de ação primeiro,
                // e dentro disso o que vence antes.
                .OrderBy(m => m.Status == StatusManutencao.Pendente ? 0 : 1)
                .ThenBy(m => m.QuilometragemPrevista)
                .ToListAsync();

        // Rastreado (sem AsNoTracking): serve tanto para leitura quanto para update/conclusão.
        public async Task<Manutencao?> GetByIdAsync(int id, int empresaId)
            => await context.Manutencoes
                .Include(m => m.Veiculo)
                .Include(m => m.Tipo)
                .FirstOrDefaultAsync(m => m.Id == id && m.EmpresaId == empresaId);

        public async Task<bool> ExisteDuplicadaAsync(int empresaId, int veiculoId, int tipoManutencaoId, int quilometragemPrevista, int? ignorarId = null)
            => await context.Manutencoes.AsNoTracking()
                .AnyAsync(m => m.EmpresaId == empresaId
                            && m.VeiculoId == veiculoId
                            && m.TipoManutencaoId == tipoManutencaoId
                            && m.QuilometragemPrevista == quilometragemPrevista
                            && m.Status == StatusManutencao.Pendente
                            && (ignorarId == null || m.Id != ignorarId));

        public async Task<bool> ExisteComTipoAsync(int empresaId, int tipoManutencaoId)
            => await context.Manutencoes.AsNoTracking()
                .AnyAsync(m => m.EmpresaId == empresaId && m.TipoManutencaoId == tipoManutencaoId);

        public async Task<Manutencao> AddAsync(Manutencao manutencao)
        {
            context.Manutencoes.Add(manutencao);
            await context.SaveChangesAsync();
            return manutencao;
        }

        public async Task<Manutencao> UpdateAsync(Manutencao manutencao)
        {
            context.Manutencoes.Update(manutencao);
            await context.SaveChangesAsync();
            return manutencao;
        }

        public async Task DeleteAsync(Manutencao manutencao)
        {
            context.Manutencoes.Remove(manutencao);
            await context.SaveChangesAsync();
        }
    }
}
