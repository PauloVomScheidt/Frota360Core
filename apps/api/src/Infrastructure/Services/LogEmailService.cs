using Frota360.Domain.Common;
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
        public Task EnviarAsync(string para, string assunto, CorpoDeEmail corpo)
        {
            // A versão em texto, e não a HTML: é ela que deixa o link legível no console.
            logger.LogWarning("[EMAIL DEV] Para: {Para} | Assunto: {Assunto}{Quebra}{Corpo}",
                para, assunto, Environment.NewLine, corpo.Texto);
            return Task.CompletedTask;
        }
    }
}
