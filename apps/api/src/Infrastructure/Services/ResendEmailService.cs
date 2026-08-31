using Frota360.Domain.Common;
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
        private const string NomeExibicao = "Frota360";

        public async Task EnviarAsync(string para, string assunto, CorpoDeEmail corpo)
        {
            var payload = new Dictionary<string, object>
            {
                // Nome de exibição junto do endereço: remetente sem nome pontua pior nos
                // filtros e chega ao destinatário como um endereço cru.
                ["from"] = $"{NomeExibicao} <{configuration["Resend:From"]}>",
                ["to"] = new[] { para },
                ["subject"] = assunto,
                ["html"] = corpo.Html,
                ["text"] = corpo.Texto,
            };

            // Opcional: um "não responda" sem destino de resposta é penalizado, mas exige
            // uma caixa que alguém leia — enquanto não houver, é melhor omitir o campo.
            var replyTo = configuration["Resend:ReplyTo"];
            if (!string.IsNullOrWhiteSpace(replyTo))
                payload["reply_to"] = replyTo;

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["Resend:ApiKey"]);

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var corpoResposta = await response.Content.ReadAsStringAsync();
                logger.LogError("Falha ao enviar e-mail via Resend. Status {Status} | Resposta: {Corpo}",
                    (int)response.StatusCode, corpoResposta);
                throw new InvalidOperationException("Não foi possível enviar o e-mail. Tente novamente mais tarde.");
            }

            logger.LogInformation("E-mail enviado para {Para} | Assunto: {Assunto}", para, assunto);
        }
    }
}
