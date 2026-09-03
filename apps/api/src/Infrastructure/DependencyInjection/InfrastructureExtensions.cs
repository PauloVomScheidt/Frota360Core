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
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection não configurada. " +
                    "Em desenvolvimento ela vem de appsettings.Development.json; em produção, da variável de ambiente " +
                    "ConnectionStrings__DefaultConnection (formato: Host=...;Port=5432;Database=...;Username=...;Password=...).");

            var jwtKey = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:Key não configurada ou muito curta (mínimo 32 caracteres). " +
                    "Em desenvolvimento use 'dotnet user-secrets set Jwt:Key <valor>'; em produção, variável de ambiente Jwt__Key.");

            ValidarConfiguracaoDeProducao(configuration);

            services.AddDbContext<Frota360DbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<IVeiculoRepository, VeiculoRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IRotaRepository, RotaRepository>();
            services.AddScoped<IConviteRepository, ConviteRepository>();
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();
            services.AddScoped<IManutencaoRepository, ManutencaoRepository>();
            services.AddScoped<ITipoManutencaoRepository, TipoManutencaoRepository>();
            services.AddScoped<IAbastecimentoRepository, AbastecimentoRepository>();
            services.AddScoped<ILogAuditoriaRepository, LogAuditoriaRepository>();
            services.AddScoped<ITipoDespesaRepository, TipoDespesaRepository>();
            services.AddScoped<IDespesaRepository, DespesaRepository>();
            services.AddScoped<ICustoRepository, CustoRepository>();
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
                        // O front não envia mais Authorization: o token vive só no cookie
                        // HttpOnly setado no login/refresh. O header continua funcionando
                        // (Scalar, curl, um cliente externo) — o cookie é só um segundo
                        // caminho para quando ele não vem.
                        OnMessageReceived = context =>
                        {
                            if (string.IsNullOrEmpty(context.Token) &&
                                context.Request.Cookies.TryGetValue(CookiesDeSessao.Token, out var token))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        },
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

        /// <summary>
        /// Falha rápido, no boot, no que hoje falharia em silêncio depois.
        ///
        /// <c>Jwt:Key</c> e a connection string já derrubam a aplicação sozinhos, mas o resto
        /// da configuração some sem alarme: CORS vazio bloqueia o front com um erro opaco no
        /// navegador, <c>Frontend:BaseUrl</c> vazio gera link de convite quebrado, e
        /// <c>Backoffice:ApiKey</c> ausente faz o provisionamento responder 401 para sempre —
        /// tornando impossível cadastrar a primeira empresa, sem nada no log explicando.
        ///
        /// Só vale em Production: em desenvolvimento é normal rodar sem Resend e sem CORS
        /// configurado, e travar o boot ali só atrapalharia.
        /// </summary>
        private static void ValidarConfiguracaoDeProducao(IConfiguration configuration)
        {
            var ambiente = configuration["ASPNETCORE_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (!string.Equals(ambiente, "Production", StringComparison.OrdinalIgnoreCase))
                return;

            var faltando = new List<string>();

            if (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() is not { Length: > 0 })
                faltando.Add("Cors__AllowedOrigins__0 (origem do front; sem ela o navegador bloqueia toda chamada)");

            if (string.IsNullOrWhiteSpace(configuration["Frontend:BaseUrl"]))
                faltando.Add("Frontend__BaseUrl (base dos links de convite e reset de senha)");

            if (string.IsNullOrWhiteSpace(configuration["Backoffice:ApiKey"]))
                faltando.Add("Backoffice__ApiKey (sem ela não é possível provisionar a primeira empresa)");

            if (faltando.Count > 0)
                throw new InvalidOperationException(
                    "Configuração de produção incompleta. Falta definir:" + Environment.NewLine +
                    string.Join(Environment.NewLine, faltando.Select(f => "  - " + f)) + Environment.NewLine +
                    "Consulte .env.example na raiz do repositório.");

            // O aviso sobre Resend ausente fica no Program.cs: esta camada não conhece
            // Serilog, e não vale acoplá-la ao logger só por causa de uma linha.
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
