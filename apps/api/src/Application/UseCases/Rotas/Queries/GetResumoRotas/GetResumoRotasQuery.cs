using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.UseCases.Rotas.Queries.GetResumoRotas
{
    /// <summary>Período obrigatório: um total de todos os tempos não responde pergunta nenhuma.</summary>
    public sealed record GetResumoRotasQuery(DateTime De, DateTime Ate) : IQuery<ResumoRotasResponse>;
}
