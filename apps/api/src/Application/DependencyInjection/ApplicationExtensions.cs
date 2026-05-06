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
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IVeiculoService, VeiculoService>();
            services.AddScoped<IRotaService, RotaService>();
            services.AddScoped<IMotoristaService, MotoristaService>();

            services.AddValidatorsFromAssembly(typeof(ApplicationExtensions).Assembly);

            return services;
        }
    }
}
