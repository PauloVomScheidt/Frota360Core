using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoAnterior
{
    public sealed class GetAbastecimentoAnteriorHandler(IAbastecimentoRepository repository,
                                                        ICurrentUserService currentUser,
                                                        ILogger<GetAbastecimentoAnteriorHandler> logger)
        : IQueryHandler<GetAbastecimentoAnteriorQuery, AbastecimentoAnteriorResponse?>
    {
        public async Task<AbastecimentoAnteriorResponse?> HandleAsync(
            GetAbastecimentoAnteriorQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando abastecimento anterior | Veículo {VeiculoId} | Odômetro {Odometro}",
                query.VeiculoId, query.Odometro);

            // Sem recorte por motorista: a referência é do veículo. O escopo de empresa continua
            // valendo, e a resposta só carrega data e odômetro (ver AbastecimentoAnteriorResponse).
            var anterior = await repository.GetAnteriorPorOdometroAsync(
                currentUser.EmpresaId, query.VeiculoId, query.Odometro, query.IgnorarId);

            if (anterior is null)
            {
                logger.LogInformation("Nenhum abastecimento anterior abaixo de {Odometro} km", query.Odometro);
                return null;
            }

            return new AbastecimentoAnteriorResponse
            {
                DataAbastecimento = anterior.DataAbastecimento,
                Odometro = anterior.Odometro
            };
        }
    }
}
