using Frota360.Domain.Common;
using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    /// <summary>
    /// A trilha é append-only: só inserção e consulta. Não há update nem delete — nem aqui,
    /// nem em endpoint algum.
    /// </summary>
    public interface ILogAuditoriaRepository
    {
        Task<LogAuditoria> AddAsync(LogAuditoria log);

        /// <summary>
        /// Página da trilha da empresa, mais recente primeiro, com o total que satisfaz o
        /// filtro (ignorando a paginação) para o rodapé da tela.
        /// </summary>
        Task<(IEnumerable<LogAuditoria> Itens, int Total)> ConsultarAsync(int empresaId, FiltroLogAuditoria filtro);
    }
}
