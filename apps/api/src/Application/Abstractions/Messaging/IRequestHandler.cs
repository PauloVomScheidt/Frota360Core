namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Handler genérico responsável por processar uma mensagem e devolver sua resposta.
    /// </summary>
    public interface IRequestHandler<in TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
