namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Representa uma operação de leitura (sem efeitos colaterais) que produz uma resposta.
    /// </summary>
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
    }
}
