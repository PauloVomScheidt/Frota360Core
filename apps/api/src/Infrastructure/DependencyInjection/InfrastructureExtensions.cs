using System.Text;
using System.Text.Json;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Repositories;
using Frota360.Domain.Interfaces.Services;
using Frota360.Infrastructure.Data;
using Frota360.Infrastructure.Repositories;
using Frota360.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Frota360.Infrastructure.DependencyInjection
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")!;

            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:Key não configurada ou muito curta (mínimo 32 caracteres). " +
                    "Em desenvolvimento use 'dotnet user-secrets set Jwt:Key <valor>'; em produção, variável de ambiente Jwt__Key.");

            services.AddDbContext<Frota360DbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IVeiculoRepository, VeiculoRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IMotoristaRepository, MotoristaRepository>();
            services.AddScoped<IRotaRepository, RotaRepository>();
            services.AddScoped<IConviteRepository, ConviteRepository>();
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();
            services.AddScoped<ITokenService, TokenService>();

            // E-mail: Resend quando a chave está configurada; em dev sem chave, loga o conteúdo no console
            if (!string.IsNullOrWhiteSpace(configuration["Resend:ApiKey"]))
                services.AddHttpClient<IEmailService, ResendEmailService>();
            else
                services.AddSingleton<IEmailService, LogEmailService>();

            // JWT
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey))
                    };

                    // 401/403 no mesmo envelope ApiResponse do resto da API
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            return EscreverEnvelopeAsync(context.Response,
                                StatusCodes.Status401Unauthorized,
                                "Não autenticado. Faça login para continuar.");
                        },
                        OnForbidden = context =>
                            EscreverEnvelopeAsync(context.Response,
                                StatusCodes.Status403Forbidden,
                                "Você não tem permissão para esta operação.")
                    };
                });

            return services;
        }

        private static Task EscreverEnvelopeAsync(HttpResponse response, int statusCode, string mensagem)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(
                ApiResponse<object>.Fail(mensagem),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return response.WriteAsync(json);
        }
    }
}
