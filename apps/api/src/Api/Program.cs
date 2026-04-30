using Frota360.Application.DependencyInjection;
using Frota360.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Serviços ──────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); 

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

// ── Middlewares ───────────────────────────────────────
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.AddHttpAuthentication("Bearer", bearer =>
    {
        bearer.Token = "SEU_TOKEN_AQUI";
    });
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();