using Frota360.Application.Interfaces;
using Frota360.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Frota360.Application.DependencyInjection
{
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IVeiculoService, VeiculoService>();
            return services;
        }
    }
}
