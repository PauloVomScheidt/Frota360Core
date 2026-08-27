namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Ponto único de entrada do CQRS: encaminha um command/query para o handler correspondente.
    /// </summary>
    public interface IDispatcher
    {
        Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}
