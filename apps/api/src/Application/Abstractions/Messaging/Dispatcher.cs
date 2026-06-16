using Microsoft.Extensions.DependencyInjection;

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

            // O handler concreto expõe HandleAsync(TRequest, CancellationToken); usamos dynamic
            // para deixar o runtime fazer o binding do tipo fechado correto.
            return ((dynamic)handler).HandleAsync((dynamic)request, cancellationToken);
        }
    }
}
