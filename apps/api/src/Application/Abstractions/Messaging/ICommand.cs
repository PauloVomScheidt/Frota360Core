namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Representa uma operação de escrita (alteração de estado) que produz uma resposta.
    /// </summary>
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
    }
}
