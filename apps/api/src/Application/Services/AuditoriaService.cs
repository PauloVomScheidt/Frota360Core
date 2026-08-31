using Frota360.Application.Common;
using Frota360.Application.Interfaces;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Frota360.Application.Services
{
    public class AuditoriaService(ILogAuditoriaRepository repository,
                                  ICurrentUserService currentUser,
                                  ILogger<AuditoriaService> logger) : IAuditoriaService
    {
        public Task RegistrarAsync(string entidade, string acao, int? entidadeId, string descricao,
                                   IEnumerable<AlteracaoCampo>? alteracoes = null)
            => GravarAsync(new LogAuditoria
            {
                EmpresaId = currentUser.EmpresaId,
                UsuarioId = currentUser.UsuarioId,
                UsuarioNome = currentUser.Nome,
                UsuarioEmail = currentUser.Email,
                UsuarioRole = currentUser.Role,
                Entidade = entidade,
                Acao = acao,
                EntidadeId = entidadeId,
                Descricao = descricao,
                Alteracoes = Serializar(alteracoes),
                DataHora = DateTime.Now,
                IpOrigem = currentUser.IpOrigem
            });

        public Task RegistrarComoAsync(int empresaId, Usuario ator, string entidade, string acao,
                                       int? entidadeId, string descricao,
                                       IEnumerable<AlteracaoCampo>? alteracoes = null)
            => GravarAsync(new LogAuditoria
            {
                EmpresaId = empresaId,
                UsuarioId = ator.Id,
                UsuarioNome = ator.Nome,
                UsuarioEmail = ator.Email,
                UsuarioRole = ator.Role,
                Entidade = entidade,
                Acao = acao,
                EntidadeId = entidadeId,
                Descricao = descricao,
                Alteracoes = Serializar(alteracoes),
                DataHora = DateTime.Now,
                IpOrigem = null // sem sessão: não há requisição autenticada de onde tirar o IP
            });

        /// <summary>
        /// A gravação roda depois de o repositório do negócio já ter dado SaveChanges, ou seja,
        /// fora daquela transação. Por isso o catch engole: perder uma linha de auditoria é
        /// ruim, mas devolver 500 numa edição que já foi persistida é pior.
        /// </summary>
        private async Task GravarAsync(LogAuditoria log)
        {
            try
            {
                await repository.AddAsync(log);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao registrar auditoria de {Acao} em {Entidade} {EntidadeId}",
                    log.Acao, log.Entidade, log.EntidadeId);
            }
        }

        private static string? Serializar(IEnumerable<AlteracaoCampo>? alteracoes)
        {
            var lista = alteracoes?.ToList();
            return lista is null || lista.Count == 0 ? null : JsonSerializer.Serialize(lista);
        }
    }
}
