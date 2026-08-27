using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Frota360.Infrastructure.Services
{
    /// <summary>Envio de e-mail transacional via Resend (https://resend.com).</summary>
    public class ResendEmailService(HttpClient httpClient,
                                    IConfiguration configuration,
                                    ILogger<ResendEmailService> logger) : IEmailService
    {
        public async Task EnviarAsync(string para, string assunto, string corpoHtml)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["Resend:ApiKey"]);
            request.Content = JsonContent.Create(new
            {
                from = configuration["Resend:From"],
                to = new[] { para },
                subject = assunto,
                html = corpoHtml
            });

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var corpo = await response.Content.ReadAsStringAsync();
                logger.LogError("Falha ao enviar e-mail via Resend. Status {Status} | Resposta: {Corpo}",
                    (int)response.StatusCode, corpo);
                throw new InvalidOperationException("Não foi possível enviar o e-mail. Tente novamente mais tarde.");
            }

            logger.LogInformation("E-mail enviado para {Para} | Assunto: {Assunto}", para, assunto);
        }
    }
}
