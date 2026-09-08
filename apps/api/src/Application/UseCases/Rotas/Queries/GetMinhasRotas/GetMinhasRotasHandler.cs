using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Rotas.Queries.GetMinhasRotas
{
    /// <summary>
    /// Rotas do motorista logado. Escopo duplo — empresa (como todo o resto) e o próprio usuário,
    /// ambos vindos do token. O motorista é o usuário: não há id de motorista separado a resolver.
    ///
    /// A tela faz duas consultas com este mesmo handler: <c>ativo=true&amp;tamanhoPagina=1</c> para
    /// a rota em andamento e <c>ativo=false</c> paginado para o histórico. Antes ela baixava tudo
    /// e separava no cliente.
    /// </summary>
    public sealed class GetMinhasRotasHandler(IRotaRepository repository, ICurrentUserService currentUser, ILogger<GetMinhasRotasHandler> logger)
        : IQueryHandler<GetMinhasRotasQuery, ResultadoPaginado<RotaResponse>>
    {
        public async Task<ResultadoPaginado<RotaResponse>> HandleAsync(
            GetMinhasRotasQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;
            var motoristaId = currentUser.UsuarioId;

            logger.LogInformation("Buscando as rotas do motorista {MotoristaId} | Página {Pagina} | Ativo {Ativo}",
                motoristaId, f.Pagina, f.Ativo);

            var (itens, total) = await repository.ConsultarDoMotoristaAsync(
                currentUser.EmpresaId, motoristaId, new FiltroRota(f.Pagina, f.TamanhoPagina, f.Ativo));

            logger.LogInformation("Foram encontradas {Quantidade} rotas do motorista {MotoristaId}, {Total} no total",
                itens.Count(), motoristaId, total);

            return new ResultadoPaginado<RotaResponse>
            {
                Itens = itens.Select(r => r.ToResponse()),
                Pagina = f.Pagina,
                TamanhoPagina = f.TamanhoPagina,
                Total = total
            };
        }
    }
}
