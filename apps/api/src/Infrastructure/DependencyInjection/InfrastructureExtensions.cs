using Frota360.Domain.Interfaces.Repositories;
using Frota360.Infrastructure.Data;
using Frota360.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Frota360.Infrastructure.DependencyInjection
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<Frota360DbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IVeiculoRepository, VeiculoRepository>();

            return services;
        }
    }
}
