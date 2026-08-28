using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Frota360.Application.UseCases.Motoristas.Queries.GetMotoristaById
{
    public sealed class GetMotoristaByIdHandler(IUsuarioRepository repository, ICurrentUserService currentUser, ILogger<GetMotoristaByIdHandler> logger)
        : IQueryHandler<GetMotoristaByIdQuery, MotoristaResponse?>
    {
        public async Task<MotoristaResponse?> HandleAsync(GetMotoristaByIdQuery query, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Buscando o motorista Id {Id}", query.Id);

            var motorista = await repository.GetMotoristaByIdAsync(query.Id, currentUser.EmpresaId);

            return motorista?.ToMotoristaResponse();
        }
    }
}
