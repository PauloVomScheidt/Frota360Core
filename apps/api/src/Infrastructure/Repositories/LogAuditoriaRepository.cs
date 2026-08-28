using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Infrastructure.Repositories
{
    public class LogAuditoriaRepository(Frota360DbContext context) : ILogAuditoriaRepository
    {
        public async Task<LogAuditoria> AddAsync(LogAuditoria log)
        {
            context.LogsAuditoria.Add(log);
            await context.SaveChangesAsync();
            return log;
        }

        public async Task<(IEnumerable<LogAuditoria> Itens, int Total)> ConsultarAsync(int empresaId, FiltroLogAuditoria filtro)
        {
            var consulta = context.LogsAuditoria.AsNoTracking()
                .Where(l => l.EmpresaId == empresaId);

            if (!string.IsNullOrWhiteSpace(filtro.Entidade))
                consulta = consulta.Where(l => l.Entidade == filtro.Entidade);

            if (!string.IsNullOrWhiteSpace(filtro.Acao))
                consulta = consulta.Where(l => l.Acao == filtro.Acao);

            if (filtro.UsuarioId.HasValue)
                consulta = consulta.Where(l => l.UsuarioId == filtro.UsuarioId.Value);

            if (filtro.De.HasValue)
                consulta = consulta.Where(l => l.DataHora >= filtro.De.Value.Date);

            // "Até" é inclusivo: quem escolhe 28/08 espera ver o que aconteceu às 23h daquele dia.
            if (filtro.Ate.HasValue)
            {
                var fimDoDia = filtro.Ate.Value.Date.AddDays(1);
                consulta = consulta.Where(l => l.DataHora < fimDoDia);
            }

            var total = await consulta.CountAsync();

            var itens = await consulta
                .OrderByDescending(l => l.DataHora)
                .ThenByDescending(l => l.Id) // desempate estável: dois registros no mesmo instante
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .ToListAsync();

            return (itens, total);
        }
    }
}
