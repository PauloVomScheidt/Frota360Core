using Frota360.Api.Services;
using Frota360.Application.Common;
using Frota360.Application.DependencyInjection;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Frota360.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frota360.Api.Tests.Composicao
{
    /// <summary>
    /// A mesma montagem de serviços do Program.cs, sem a parte HTTP (CORS, rate limit, OpenAPI),
    /// sobre uma configuração em memória.
    /// </summary>
    internal static class Composicao
    {
        public const string ChaveJwt = "chave-de-teste-com-bem-mais-de-32-caracteres";
        public const string Emissor = "Frota360";
        public const string Audiencia = "Frota360Web";

        public static Dictionary<string, string?> ConfiguracaoMinima() => new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=frota360_teste;Username=teste;Password=teste",
            ["Jwt:Key"] = ChaveJwt,
            ["Jwt:Issuer"] = Emissor,
            ["Jwt:Audience"] = Audiencia,
        };

        public static IConfiguration Configuracao(Dictionary<string, string?> valores) =>
            new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

        public static ServiceCollection Servicos(Dictionary<string, string?>? valores = null)
        {
            var configuracao = Configuracao(valores ?? ConfiguracaoMinima());
            var services = new ServiceCollection();

            services.AddSingleton(configuracao);
            services.AddLogging();
            services.AddInfrastructure(configuracao);
            services.AddApplication();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddSingleton(new FrontendSettings("http://localhost:5173"));
            services.AddSingleton(new GoogleRoutesSettings(string.Empty));

            return services;
        }

        public static ServiceProvider Provedor(Dictionary<string, string?>? valores = null) =>
            Servicos(valores).BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
