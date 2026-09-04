using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Manutencoes.Queries.GetAllManutencoes
{
    public sealed class GetAllManutencoesHandler(IManutencaoRepository repository, ICurrentUserService currentUser, ILogger<GetAllManutencoesHandler> logger)
        : IQueryHandler<GetAllManutencoesQuery, ResultadoPaginado<ManutencaoResponse>>
    {
        public async Task<ResultadoPaginado<ManutencaoResponse>> HandleAsync(
            GetAllManutencoesQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;

            logger.LogInformation("Buscando manutenções | Página {Pagina} | Veículo {VeiculoId} | Status {Status} | De {De} | Até {Ate}",
                f.Pagina, f.VeiculoId, f.Status, f.De, f.Ate);

            // Intervalo invertido devolveria lista vazia sem explicar o porquê. O validator do
            // controller já barra isso, mas a regra fica aqui também: o handler é chamado por
            // quem não passa pelo controller (testes, e um dia outro caminho).
            if (f.De is not null && f.Ate is not null && f.Ate < f.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            var filtro = new FiltroManutencao(f.Pagina, f.TamanhoPagina, f.VeiculoId, f.Status, f.De, f.Ate);

            var (itens, total) = await repository.ConsultarAsync(currentUser.EmpresaId, filtro);

            logger.LogInformation("Foram encontradas {Quantidade} manutenções na página, {Total} no total",
                itens.Count(), total);

            return new ResultadoPaginado<ManutencaoResponse>
            {
                Itens = itens.Select(m => m.ToResponse().SemCustoParaMotorista(currentUser)),
                Pagina = f.Pagina,
                TamanhoPagina = f.TamanhoPagina,
                Total = total
            };
        }
    }
}
