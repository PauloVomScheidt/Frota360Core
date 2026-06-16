using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Motorista.Response;

namespace Frota360.Application.UseCases.Motoristas.Queries.GetAllMotoristas
{
    public sealed record GetAllMotoristasQuery : IQuery<IEnumerable<MotoristaResponse>>;
}
