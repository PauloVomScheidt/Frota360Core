using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetResumoAbastecimentos
{
    public sealed class GetResumoAbastecimentosHandler(IAbastecimentoRepository repository,
                                                       ICurrentUserService currentUser,
                                                       ILogger<GetResumoAbastecimentosHandler> logger)
        : IQueryHandler<GetResumoAbastecimentosQuery, ResumoAbastecimentosResponse>
    {
        public async Task<ResumoAbastecimentosResponse> HandleAsync(
            GetResumoAbastecimentosQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;

            logger.LogInformation("Resumindo abastecimentos | Veículo {VeiculoId} | Motorista {MotoristaId} | De {De} | Até {Ate}",
                f.VeiculoId, f.MotoristaId, f.De, f.Ate);

            if (f.De is not null && f.Ate is not null && f.Ate < f.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            // ⚠️ O mesmo filtro da listagem, inclusive o recorte do motorista: sem ele o rodapé
            // entregaria ao motorista o total da empresa.
            var filtro = GetAllAbastecimentosHandler.FiltroDoUsuario(f, currentUser);

            var resumo = await repository.ResumirAsync(currentUser.EmpresaId, filtro);

            logger.LogInformation("Resumo: {Quantidade} lançamentos somando {ValorTotal}",
                resumo.Quantidade, resumo.ValorTotal);

            return new ResumoAbastecimentosResponse
            {
                Quantidade = resumo.Quantidade,
                ValorTotal = resumo.ValorTotal
            };
        }
    }
}
