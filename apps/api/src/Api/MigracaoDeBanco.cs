using Frota360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Frota360.Api
{
    /// <summary>
    /// Aplica as migrations pendentes na inicialização.
    ///
    /// Faz sentido aqui porque o deploy é uma réplica única em Docker Compose: não há passo
    /// de pipeline separado, e o banco não é alcançável de fora da rede do compose para se
    /// rodar <c>dotnet ef database update</c> à mão.
    ///
    /// <b>Limitação a conhecer:</b> isto é aplicação automática, sem revisão humana. Uma
    /// migration destrutiva sobe sozinha, não há gate nem rollback, e enquanto ela roda a API
    /// ainda não atende — com réplica única, isso é uma janela curta de indisponibilidade. No
    /// dia em que houver mais de uma réplica, duas instâncias subindo juntas disputariam a
    /// mesma migration e este método precisará de trava (advisory lock do Postgres).
    /// </summary>
    public static class MigracaoDeBanco
    {
        // O depends_on: service_healthy do compose só segura a PRIMEIRA subida. Se o Postgres
        // reiniciar depois, a API sobe sem ele de pé — e com restart: unless-stopped isso
        // viraria loop de reinício. O retry cobre essa janela.
        private const int TentativasMaximas = 10;

        public static async Task AplicarAsync(WebApplication app)
        {
            // Development aplica migration à mão (dotnet ef database update) e Testing tem a
            // própria fixture; rodar aqui nesses ambientes só criaria surpresa.
            if (!app.Environment.IsProduction() && !app.Environment.IsStaging())
            {
                app.Logger.LogInformation(
                    "Migrations no boot desativadas em {Ambiente}: use 'dotnet ef database update'.",
                    app.Environment.EnvironmentName);
                return;
            }

            using var escopo = app.Services.CreateScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<Frota360DbContext>();

            for (var tentativa = 1; tentativa <= TentativasMaximas; tentativa++)
            {
                try
                {
                    var pendentes = (await contexto.Database.GetPendingMigrationsAsync()).ToList();

                    if (pendentes.Count == 0)
                    {
                        app.Logger.LogInformation("Banco já está atualizado, nenhuma migration pendente.");
                        return;
                    }

                    app.Logger.LogInformation("Aplicando {Total} migration(s) pendente(s): {Migrations}",
                        pendentes.Count, string.Join(", ", pendentes));

                    await contexto.Database.MigrateAsync();

                    app.Logger.LogInformation("Migrations aplicadas com sucesso.");
                    return;
                }
                catch (Exception ex) when (tentativa < TentativasMaximas)
                {
                    // Backoff linear até 10s. O caso comum é o Postgres ainda subindo, que se
                    // resolve em segundos; um erro real de migration esgota as tentativas e
                    // derruba a aplicação com a exceção original, que é o comportamento certo.
                    var espera = TimeSpan.FromSeconds(Math.Min(tentativa, 10));

                    app.Logger.LogWarning(ex,
                        "Falha ao aplicar migrations (tentativa {Tentativa}/{Total}). Nova tentativa em {Espera}s.",
                        tentativa, TentativasMaximas, espera.TotalSeconds);

                    await Task.Delay(espera);
                }
            }

            // Última tentativa fora do catch: se falhar aqui, a exceção sobe e o container
            // morre com a causa no log, em vez de servir requisições contra um banco errado.
            await contexto.Database.MigrateAsync();
            app.Logger.LogInformation("Migrations aplicadas com sucesso na última tentativa.");
        }
    }
}
