namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Marcador base para qualquer mensagem (command ou query) que produz uma resposta.
    /// </summary>
    public interface IRequest<out TResponse>
    {
    }
}
