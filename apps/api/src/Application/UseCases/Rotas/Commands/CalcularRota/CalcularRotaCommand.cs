using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.DTOs.Rota.Response;

namespace Frota360.Application.UseCases.Rotas.Commands.CalcularRota
{
    /// <summary>
    /// É <see cref="ICommand{TResponse}"/>, e não <c>IQuery</c>, apesar de não alterar estado
    /// nosso: a chamada é cobrada pela Google e registrada na trilha, então tem efeito
    /// colateral fora do banco — tratá-la como leitura convidaria a repeti-la à vontade.
    /// </summary>
    public sealed record CalcularRotaCommand(CalcularRotaRequest Data) : ICommand<CalculoRotaResponse>;
}
