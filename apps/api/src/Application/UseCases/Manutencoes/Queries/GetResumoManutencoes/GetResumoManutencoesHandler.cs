using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetResumoManutencoes
{
    /// <summary>
    /// "Atrasada" e "km restantes" só existem depois do <c>ToResponse()</c>, que compara com o
    /// odômetro atual — não há coluna para filtrar no banco. Por isso a contagem é feita sobre as
    /// pendentes em memória, um conjunto limitado pela frota.
    /// </summary>
    public sealed class GetResumoManutencoesHandler(IManutencaoRepository repository,
                                                    ICurrentUserService currentUser,
                                                    ILogger<GetResumoManutencoesHandler> logger)
        : IQueryHandler<GetResumoManutencoesQuery, ResumoManutencoesResponse>
    {
        public async Task<ResumoManutencoesResponse> HandleAsync(
            GetResumoManutencoesQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Resumindo manutenções pendentes");

                var pendentes = (await repository.ListarPendentesAsync(currentUser.EmpresaId))
                    .Select(m => m.ToResponse())
                    .ToList();

                var atrasadas = pendentes.Where(m => m.Atrasada).ToList();
                var vencendo = pendentes
                    .Where(m => !m.Atrasada && m.KmRestantes <= RegrasDeManutencao.FaixaAvisoKm)
                    .ToList();

                logger.LogInformation("Resumo: {Atrasadas} atrasadas e {Vencendo} vencendo de {Pendentes} pendentes",
                    atrasadas.Count, vencendo.Count, pendentes.Count);

                return new ResumoManutencoesResponse
                {
                    Atrasadas = atrasadas.Count,
                    Vencendo = vencendo.Count,
                    Urgentes = atrasadas.OrderBy(m => m.KmRestantes).ThenBy(m => m.Id)
                        .Concat(vencendo.OrderBy(m => m.KmRestantes).ThenBy(m => m.Id))
                        .Take(RegrasDeManutencao.LimiteUrgentes)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao resumir manutenções pendentes");
                throw;
            }
        }
    }
}
