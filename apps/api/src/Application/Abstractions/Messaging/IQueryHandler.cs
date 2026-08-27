namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Handler especializado em processar uma <see cref="IQuery{TResponse}"/>.
    /// </summary>
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
    }
}
