using Frota360.Domain.Entities;
using Frota360.Domain.Enums;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class ManutencaoRepository(Frota360DbContext context) : IManutencaoRepository
    {
        public async Task<IEnumerable<Manutencao>> GetAllAsync(int empresaId, int? veiculoId = null,
            StatusManutencao? status = null, DateTime? de = null, DateTime? ate = null)
        {
            var consulta = context.Manutencoes.AsNoTracking()
                .Include(m => m.Veiculo)
                .Include(m => m.Tipo)
                .Where(m => m.EmpresaId == empresaId);

            if (veiculoId is not null)
                consulta = consulta.Where(m => m.VeiculoId == veiculoId);

            if (status is not null)
                consulta = consulta.Where(m => m.Status == status);

            // A data que importa depende do status: uma pendência é situada pelo prazo,
            // uma manutenção feita pela execução. As duas pernas do OR ficam explícitas
            // (em vez de um ternário) porque `Status` tem conversão para texto e a
            // comparação simples é a que o EF traduz sem surpresa.
            //
            // Consequência aceita: pendência agendada só por km, sem DataPrevista, não
            // aparece quando há filtro de período — ela não está em data nenhuma.
            if (de is not null)
            {
                var inicio = de.Value.Date;
                consulta = consulta.Where(m =>
                    (m.Status == StatusManutencao.Pendente && m.DataPrevista >= inicio)
                    || (m.Status != StatusManutencao.Pendente && m.DataRealizacao >= inicio));
            }

            // "Até" é inclusivo: quem escolhe 11/08 espera ver o que caiu às 23h daquele dia.
            if (ate is not null)
            {
                var fim = ate.Value.Date.AddDays(1);
                consulta = consulta.Where(m =>
                    (m.Status == StatusManutencao.Pendente && m.DataPrevista < fim)
                    || (m.Status != StatusManutencao.Pendente && m.DataRealizacao < fim));
            }

            // Status é persistido como texto, então OrderBy(Status) sairia em ordem
            // alfabética. O que a tela quer é o que ainda precisa de ação primeiro,
            // e dentro disso o que vence antes.
            return await consulta
                .OrderBy(m => m.Status == StatusManutencao.Pendente ? 0 : 1)
                .ThenBy(m => m.QuilometragemPrevista)
                .ToListAsync();
        }

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
