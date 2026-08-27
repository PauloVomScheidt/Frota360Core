using Frota360.Application.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Frota360.Tests.Abstractions
{
    public class DispatcherTests
    {
        // Mensagens e handlers de apoio usados apenas nos testes do dispatcher.
        private sealed record PingQuery(string Texto) : IQuery<string>;

        private sealed class PingHandler : IQueryHandler<PingQuery, string>
        {
            public Task<string> HandleAsync(PingQuery request, CancellationToken cancellationToken = default)
                => Task.FromResult($"pong: {request.Texto}");
        }

        private sealed record SemHandlerQuery : IQuery<int>;

        private static IDispatcher BuildDispatcher(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            services.AddScoped<IDispatcher, Dispatcher>();
            configure(services);
            return services.BuildServiceProvider().GetRequiredService<IDispatcher>();
        }

        [Fact]
        public async Task SendAsync_DeveRotear_ParaHandlerRegistrado()
        {
            var dispatcher = BuildDispatcher(s =>
                s.AddScoped<IRequestHandler<PingQuery, string>, PingHandler>());

            var resultado = await dispatcher.SendAsync(new PingQuery("oi"));

            Assert.Equal("pong: oi", resultado);
        }

        [Fact]
        public async Task SendAsync_SemHandlerRegistrado_DeveLancarInvalidOperationException()
        {
            var dispatcher = BuildDispatcher(_ => { });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => dispatcher.SendAsync(new SemHandlerQuery()));

            Assert.Contains(nameof(SemHandlerQuery), ex.Message);
        }

        [Fact]
        public async Task SendAsync_RequestNulo_DeveLancarArgumentNullException()
        {
            var dispatcher = BuildDispatcher(_ => { });

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => dispatcher.SendAsync<string>(null!));
        }
    }
}
