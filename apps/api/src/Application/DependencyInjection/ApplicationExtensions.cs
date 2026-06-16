using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.Interfaces;
using Frota360.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace Frota360.Application.DependencyInjection
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Auth permanece como serviço (fluxo de token próprio)
            services.AddScoped<IAuthService, AuthService>();

            // Infraestrutura CQRS manual
            services.AddScoped<IDispatcher, Dispatcher>();
            services.AddCqrsHandlers();

            services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

            return services;
        }

        /// <summary>
        /// Varre a assembly da Application e registra todas as implementações de
        /// <see cref="IRequestHandler{TRequest,TResponse}"/> pelo seu contrato fechado,
        /// permitindo que o <see cref="Dispatcher"/> as resolva via DI.
        /// </summary>
        private static IServiceCollection AddCqrsHandlers(this IServiceCollection services)
        {
            var assembly = typeof(ApplicationExtensions).Assembly;

            var handlers = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType
                                && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    .Select(i => new { Service = i, Implementation = t }));

            foreach (var handler in handlers)
                services.AddScoped(handler.Service, handler.Implementation);

            return services;
        }
    }
}
