namespace Frota360.Application.Abstractions.Messaging
{
    /// <summary>
    /// Implementação manual do dispatcher: resolve o <see cref="IRequestHandler{TRequest,TResponse}"/>
    /// fechado a partir do contêiner de DI e invoca seu <c>HandleAsync</c>.
    /// </summary>
    public sealed class Dispatcher(IServiceProvider provider) : IDispatcher
    {
        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));

            var handler = provider.GetService(handlerType)
                ?? throw new InvalidOperationException(
                    $"Nenhum handler registrado para '{request.GetType().Name}'.");

            // Invocamos via MethodInfo da interface (e não do tipo concreto): o despacho de
            // interface independe da visibilidade do handler e dispensa o uso de 'dynamic'.
            var handleAsync = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))!;

            return (Task<TResponse>)handleAsync.Invoke(handler, [request, cancellationToken])!;
        }
    }
}
