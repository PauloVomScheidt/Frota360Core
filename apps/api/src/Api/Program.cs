using Asp.Versioning;
using Frota360.Api.Middlewares;
using Frota360.Application.DependencyInjection;
using Frota360.Infrastructure.Data;
using Frota360.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Iniciando Frota360 API...");

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/frota360-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    ));

// Serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// CORS — origens permitidas vêm de Cors:AllowedOrigins (appsettings por ambiente)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
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

// Health Check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Frota360DbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: ["db", "sql"]);

var app = builder.Build();

// Middlewares
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000}ms";
});
// CORS precisa vir antes do rate limiter para que respostas 429 também levem os headers
app.UseCors();
app.UseRateLimiter();
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.AddHttpAuthentication("Bearer", bearer =>
    {
        bearer.Token = "SEU_TOKEN_AQUI";
    });
});

// Endpoints de Health Check
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var resultado = new
        {
            status = report.Status.ToString(),
            duracao = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                nome = e.Key,
                status = e.Value.Status.ToString(),
                duracao = e.Value.Duration,
                erro = e.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsJsonAsync(resultado);
    }
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();