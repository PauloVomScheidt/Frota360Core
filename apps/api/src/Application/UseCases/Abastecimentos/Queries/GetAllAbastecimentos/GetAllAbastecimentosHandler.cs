using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAllAbastecimentos
{
    public sealed class GetAllAbastecimentosHandler(IAbastecimentoRepository repository,
                                                    ICurrentUserService currentUser,
                                                    ILogger<GetAllAbastecimentosHandler> logger)
        : IQueryHandler<GetAllAbastecimentosQuery, ResultadoPaginado<AbastecimentoResponse>>
    {
        public async Task<ResultadoPaginado<AbastecimentoResponse>> HandleAsync(
            GetAllAbastecimentosQuery query, CancellationToken cancellationToken = default)
        {
            var f = query.Filtro;

            logger.LogInformation("Buscando abastecimentos | Página {Pagina} | Veículo {VeiculoId} | Motorista {MotoristaId} | De {De} | Até {Ate}",
                f.Pagina, f.VeiculoId, f.MotoristaId, f.De, f.Ate);

            if (f.De is not null && f.Ate is not null && f.Ate < f.De)
                throw new InvalidOperationException("A data final do período não pode ser anterior à inicial.");

            var filtro = FiltroDoUsuario(f, currentUser);

            var (itens, total) = await repository.ConsultarAsync(currentUser.EmpresaId, filtro);

            logger.LogInformation("Foram encontrados {Quantidade} abastecimentos na página, {Total} no total",
                itens.Count(), total);

            return new ResultadoPaginado<AbastecimentoResponse>
            {
                Itens = itens.Select(a => a.ToResponse()),
                Pagina = filtro.Pagina,
                TamanhoPagina = filtro.TamanhoPagina,
                Total = total
            };
        }

        /// <summary>
        /// Monta o filtro do repositório aplicando o segundo eixo.
        ///
        /// ⚠️ O recorte do motorista entra <b>no filtro</b>, e não depois da consulta: é o que faz
        /// o <c>COUNT</c> da paginação — e o total do rodapé, que sai do mesmo filtro — também
        /// sair recortado. Aplicado depois, o motorista veria o volume da empresa inteira.
        ///
        /// Compartilhado com o handler do resumo para que a listagem e o rodapé nunca divirjam.
        /// </summary>
        internal static FiltroAbastecimento FiltroDoUsuario(
            DTOs.Abastecimento.Request.ConsultarAbastecimentosRequest f, ICurrentUserService currentUser)
            => new(
                f.Pagina,
                f.TamanhoPagina,
                f.VeiculoId,
                currentUser.EhMotorista() ? currentUser.UsuarioId : f.MotoristaId,
                f.De,
                f.Ate);
    }
}
