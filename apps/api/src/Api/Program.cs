using Asp.Versioning;
using Frota360.Api;
using Frota360.Api.Middlewares;
using Frota360.Api.Services;
using Frota360.Application.Common;
using Frota360.Application.DependencyInjection;
using Frota360.Application.Interfaces;
using Frota360.Infrastructure.Data;
using Frota360.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Iniciando Frota360 API...");

// O sistema grava hora local (DateTime.Now) em todas as datas persistidas, então o fuso do
// processo faz parte da semântica dos dados, não é detalhe de ambiente. Em container sem
// TZ definida o Linux assume UTC, e o mesmo código passaria a gravar 3 h adiantado sem
// quebrar nada — por isso o fuso efetivo é registrado logo na primeira linha do log.
Log.Information("Fuso horário do processo: {Fuso} (UTC{Offset:+00;-00}). Esperado em produção: America/Sao_Paulo (UTC-03).",
    TimeZoneInfo.Local.Id, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours);

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    // Arquivo so fora de producao. Em container o log em arquivo se perde a cada deploy, e
    // escrever em disco impediria rodar como usuario sem privilegio (nao ha logs/ com
    // permissao). Em producao o destino e o stdout, coletado pelo Docker — com rotacao
    // configurada no compose, senao o json-file cresce sem teto e enche o disco da EC2.
    if (!context.HostingEnvironment.IsProduction())
    {
        configuration.WriteTo.File(
            path: "logs/frota360-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
    }
});

// Serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    // Declara o esquema "Bearer" no documento OpenAPI — sem isso o Scalar
    // não sabe anexar o header Authorization nas requisições de teste.
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
        };
        return Task.CompletedTask;
    });

    // Marca como protegidas (no Swagger/Scalar) apenas as operações com [Authorize].
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var exigeAuth = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<AuthorizeAttribute>().Any();

        if (exigeAuth)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
        }

        return Task.CompletedTask;
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Sem chave do Resend o sistema cai no LogEmailService de propósito — prático em dev, mas em
// produção significa convite e reset de senha que nunca chegam ao destinatário. Não é motivo
// para derrubar o boot, é motivo para aparecer no log.
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(builder.Configuration["Resend:ApiKey"]))
    Log.Warning("Resend:ApiKey não configurada em Production: os e-mails de convite e reset de " +
                "senha NÃO serão enviados — apenas registrados no log.");

// Usuário atual (claims do JWT) disponível para os handlers
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// URL do front, usada nos links de convite/reset enviados por e-mail
builder.Services.AddSingleton(new FrontendSettings(builder.Configuration["Frontend:BaseUrl"] ?? string.Empty));

// CORS — origens permitidas vêm de Cors:AllowedOrigins (appsettings por ambiente)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

//Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Política global — aplicada em todos os endpoints
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new
        {
            sucesso = false,
            mensagem = "Muitas requisições. Tente novamente em instantes.",
            dados = (object?)null,
            erros = (IEnumerable<string>?)null
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };
});

// Versionamento
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),        
        new HeaderApiVersionReader("api-version"), 
        new QueryStringApiVersionReader("version") 
    );
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Headers do proxy reverso (Caddy).
//
// Em producao a API so e alcancada pelo Caddy, entao TODA requisicao chega com o IP do
// container do proxy. Sem tratar os X-Forwarded-*, tres coisas quebram em silencio:
//
//   1. LogAuditoria.IpOrigem grava o IP do Caddy em todo registro, inutilizando a trilha;
//   2. o rate limiter particiona por esse mesmo IP unico, entao o limite de 5/min da
//      politica "auth" vira um teto COMPARTILHADO por todos os usuarios do sistema;
//   3. CreatedAtAction monta o header Location a partir de Request.Scheme/Host e devolve
//      ao cliente a URL interna do Docker (http://api:8080/...).
//
// Restringir a confianca e obrigatorio: aceitar X-Forwarded-For de qualquer origem deixa
// qualquer cliente forjar o proprio IP e escapar do rate limit. A trava e dupla — o
// middleware so aceita os headers vindos de KnownNetworks, e a porta 8080 da API nunca e
// publicada no host, entao o unico caminho ate ela e o proxy.
//
// ATENCAO ao padrao do ASP.NET: KnownNetworks vem com 127.0.0.1/8 e KnownProxies com ::1.
// No Docker o Caddy e outro container, com IP de bridge — nao loopback. Sem limpar as
// listas e declarar a sub-rede do compose, o middleware IGNORA os headers sem avisar e o
// problema continua parecendo resolvido.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Um unico proxy na frente (o Caddy). Impede que um X-Forwarded-For encadeado pelo
    // cliente empurre o IP real para fora da janela lida.
    options.ForwardLimit = 1;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // Sub-rede fixada em docker-compose.prod.yml. Em desenvolvimento nao ha proxy e a
    // variavel nao existe, entao nada e confiado — que e o comportamento correto.
    var redeDoProxy = builder.Configuration["ProxyReverso:RedeConfiavel"];
    if (!string.IsNullOrWhiteSpace(redeDoProxy))
    {
        // KnownNetworks usa o IPNetwork do HttpOverrides, que não tem Parse — daí a
        // separação manual do CIDR.
        var partes = redeDoProxy.Split('/', StringSplitOptions.RemoveEmptyEntries);
        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
            System.Net.IPAddress.Parse(partes[0]), int.Parse(partes[1])));
        Log.Information("Confiando nos headers de proxy vindos de {Rede}", redeDoProxy);
    }
});

// Health Check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Frota360DbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["db", "postgres"]);

var app = builder.Build();

// Antes de qualquer middleware: se o schema estiver desatualizado, não faz sentido começar
// a atender. Só roda em Production/Staging — ver MigracaoDeBanco.
await MigracaoDeBanco.AplicarAsync(app);

// Middlewares
//
// UseForwardedHeaders vem PRIMEIRO: ele reescreve Connection.RemoteIpAddress e
// Request.Scheme, e tudo o que vier depois — log do Serilog, rate limiter, auditoria,
// geracao de URL — precisa ja enxergar os valores corrigidos.
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000}ms";
});
// CORS precisa vir antes do rate limiter para que respostas 429 também levem os headers
app.UseCors();
app.UseRateLimiter();
// Documentacao interativa apenas em desenvolvimento: em producao ela publicaria o mapa
// completo de endpoints (inclusive os de backoffice) para qualquer um que sondasse a API.
// Nao e falha de acesso — tudo que importa exige JWT — mas e reconhecimento de graca.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddHttpAuthentication("Bearer", bearer =>
        {
            bearer.Token = "SEU_TOKEN_AQUI";
        });
    });
}

// Endpoints de Health Check
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        foreach (var falha in report.Entries.Where(e => e.Value.Exception is not null))
            Log.Error(falha.Value.Exception, "Health check {Check} falhou", falha.Key);

        var resultado = new
        {
            status = report.Status.ToString(),
            duracao = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                nome = e.Key,
                status = e.Value.Status.ToString(),
                duracao = e.Value.Duration,
                // A mensagem da excecao NAO sai na resposta: numa falha do DbContext o
                // Npgsql descreve host, banco e usuario, e este endpoint e publico. O
                // detalhe vai para o log, onde ha controle de acesso.
                erro = e.Value.Exception is null ? null : "Consulte o log do servidor."
            })
        };

        await context.Response.WriteAsJsonAsync(resultado);
    }
});

// Só em desenvolvimento, onde o Kestrel realmente escuta em HTTPS. Em produção quem termina
// o TLS é o Caddy, que já redireciona http->https sozinho; a API atrás dele fala HTTP puro na
// 8080. Manter o middleware ali só produzia o aviso "Failed to determine the https port" a
// cada boot e, se alguém definisse ASPNETCORE_HTTPS_PORT, viraria loop de redirect.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();