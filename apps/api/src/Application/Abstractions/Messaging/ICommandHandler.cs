namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Handler especializado em processar um <see cref="ICommand{TResponse}"/>.
    /// </summary>
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
    }
}
