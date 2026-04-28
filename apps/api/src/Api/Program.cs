using Microsoft.OpenApi;
using Frota360.Application.DependencyInjection;
using Frota360.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Serviços ──────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minha API",
        Version = "v1",
        Description = "Documentação da API com SOLID"
    });

    // Suporte a JWT no Swagger (se usar autenticação)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT: Bearer {seu_token}"
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();

var app = builder.Build();

// ── Middlewares ───────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Minha API v1");
        options.RoutePrefix = string.Empty; // Abre na raiz: http://localhost:5000/
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();