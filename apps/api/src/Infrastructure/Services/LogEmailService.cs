using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Frota360.Infrastructure.Services
{
    /// <summary>
    /// Implementação de desenvolvimento: não envia nada, apenas loga o conteúdo
    /// (o link de convite/reset aparece no console). Usada quando Resend:ApiKey não está configurada.
    /// </summary>
    public class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
    {
        public Task EnviarAsync(string para, string assunto, string corpoHtml)
        {
            logger.LogWarning("[EMAIL DEV] Para: {Para} | Assunto: {Assunto} | Corpo: {Corpo}", para, assunto, corpoHtml);
            return Task.CompletedTask;
        }
    }
}
