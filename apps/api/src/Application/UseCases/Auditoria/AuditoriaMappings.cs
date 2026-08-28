using Frota360.Application.Common;
using Frota360.Application.DTOs.Auditoria.Response;
using Frota360.Domain.Entities;
using System.Text.Json;

namespace Frota360.Application.UseCases.Auditoria
{
    /// <summary>Mapeamento centralizado de <see cref="LogAuditoria"/> para <see cref="LogAuditoriaResponse"/>.</summary>
    public static class AuditoriaMappings
    {
        public static LogAuditoriaResponse ToResponse(this LogAuditoria log) => new()
        {
            Id = log.Id,
            UsuarioId = log.UsuarioId,
            UsuarioNome = log.UsuarioNome,
            UsuarioEmail = log.UsuarioEmail,
            UsuarioRole = log.UsuarioRole,
            Entidade = log.Entidade,
            Acao = log.Acao,
            EntidadeId = log.EntidadeId,
            Descricao = log.Descricao,
            Alteracoes = DesserializarAlteracoes(log.Alteracoes),
            DataHora = log.DataHora,
            IpOrigem = log.IpOrigem
        };

        /// <summary>
        /// JSON malformado (linha antiga, escrita à mão no banco) vira lista vazia em vez de
        /// derrubar a listagem inteira: a trilha é histórico e não vale perder o resto por
        /// causa de uma linha.
        /// </summary>
        private static IReadOnlyList<AlteracaoCampo> DesserializarAlteracoes(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];

            try
            {
                return JsonSerializer.Deserialize<List<AlteracaoCampo>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
