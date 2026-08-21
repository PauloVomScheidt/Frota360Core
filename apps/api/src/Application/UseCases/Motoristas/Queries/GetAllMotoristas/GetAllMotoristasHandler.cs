using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Motoristas;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Motoristas.Queries.GetAllMotoristas
{
    public sealed class GetAllMotoristasHandler(IMotoristaRepository repository, ICurrentUserService currentUser, ILogger<GetAllMotoristasHandler> logger)
        : IQueryHandler<GetAllMotoristasQuery, IEnumerable<MotoristaResponse>>
    {
        public async Task<IEnumerable<MotoristaResponse>> HandleAsync(GetAllMotoristasQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando todos os motoristas");

            var motoristas = await repository.GetAllAsync(currentUser.EmpresaId);

            logger.LogInformation("Foram encontrados {QuantidadeMotoristas} motoristas", motoristas.Count());

            return motoristas.Select(m => m.ToResponse());
        }
    }
}
