using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Frota360.Infrastructure.Services
{
    /// <summary>
    /// Cliente da Google Routes API (https://routes.googleapis.com).
    ///
    /// Nenhum caminho aqui propaga exceção: o cálculo de trajeto é conveniência sobre um
    /// serviço de terceiro, e derrubar a requisição do usuário porque a Google demorou seria
    /// trocar um recurso ausente por uma tela quebrada.
    /// </summary>
    public class GoogleRoutesClient(HttpClient httpClient,
                                    GoogleRoutesSettings settings,
                                    ILogger<GoogleRoutesClient> logger) : IGoogleRoutesClient
    {
        private const string Endpoint = "https://routes.googleapis.com/directions/v2:computeRoutes";

        /// <summary>
        /// Pedir campo a mais não é só payload maior: a combinação de máscara e preferência de
        /// rota é o que determina o SKU cobrado pela Google. Estes três, com
        /// TRAFFIC_UNAWARE abaixo, ficam na faixa mais barata — <b>não amplie sem checar preço</b>.
        /// </summary>
        private const string MascaraDeCampos = "routes.duration,routes.distanceMeters,routes.polyline.encodedPolyline";

        public async Task<ResultadoCalculoRota> CalcularAsync(Coordenada origem, Coordenada destino,
                                                              CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                logger.LogWarning("GoogleRoutes:ApiKey não configurada — cálculo de rota indisponível.");
                return ResultadoCalculoRota.Falha("Chave da Routes API não configurada.");
            }

            var payload = new
            {
                origin = PontoDe(origem),
                destination = PontoDe(destino),
                travelMode = "DRIVE",
                // TRAFFIC_UNAWARE de propósito: considerar trânsito ao vivo muda o SKU para uma
                // faixa mais cara, e a estimativa aqui é de planejamento, não de navegação.
                routingPreference = "TRAFFIC_UNAWARE",
                languageCode = "pt-BR",
                units = "METRIC"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Goog-Api-Key", settings.ApiKey);
            request.Headers.Add("X-Goog-FieldMask", MascaraDeCampos);

            try
            {
                var response = await httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var corpoErro = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("Routes API respondeu {Status} | Corpo: {Corpo}", (int)response.StatusCode, corpoErro);
                    return ResultadoCalculoRota.Falha($"Routes API respondeu {(int)response.StatusCode}.");
                }

                var conteudo = await response.Content.ReadFromJsonAsync<RespostaRoutes>(cancellationToken);
                var rota = conteudo?.Routes?.FirstOrDefault();

                // 200 com "routes" vazio é a resposta normal da Google para "não há trajeto
                // entre estes dois pontos" — pontos em ilhas diferentes, por exemplo.
                if (rota is null)
                {
                    logger.LogInformation("Routes API não encontrou trajeto entre os pontos informados.");
                    return ResultadoCalculoRota.Falha("Nenhum trajeto encontrado entre os pontos informados.");
                }

                var duracao = SegundosDe(rota.Duration);
                if (duracao is null)
                {
                    logger.LogError("Duração em formato inesperado na resposta da Routes API: {Duracao}", rota.Duration);
                    return ResultadoCalculoRota.Falha("Duração em formato inesperado na resposta da Routes API.");
                }

                logger.LogInformation("Trajeto calculado: {Metros} m em {Segundos} s", rota.DistanceMeters, duracao);

                return ResultadoCalculoRota.Ok(rota.DistanceMeters, duracao.Value, rota.Polyline?.EncodedPolyline);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout do HttpClient chega como TaskCanceledException. A guarda separa isso
                // do cancelamento legítimo da requisição, que não é falha nossa a registrar.
                logger.LogError(ex, "Timeout ao chamar a Routes API.");
                return ResultadoCalculoRota.Falha("Tempo esgotado ao consultar a Routes API.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Falha de rede ao chamar a Routes API.");
                return ResultadoCalculoRota.Falha("Falha de rede ao consultar a Routes API.");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Resposta da Routes API em formato inesperado.");
                return ResultadoCalculoRota.Falha("Resposta da Routes API em formato inesperado.");
            }
        }

        private static object PontoDe(Coordenada c) =>
            new { location = new { latLng = new { latitude = c.Latitude, longitude = c.Longitude } } };

        /// <summary>
        /// A Routes API devolve duração como Duration do protobuf serializada em texto —
        /// "1234s", segundos com sufixo e possível parte fracionária ("1234.5s"). Arredonda
        /// para o segundo inteiro, que é a resolução que o domínio guarda.
        /// </summary>
        private static int? SegundosDe(string? duracao)
        {
            if (string.IsNullOrWhiteSpace(duracao) || !duracao.EndsWith('s'))
                return null;

            return double.TryParse(duracao[..^1], System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out var segundos)
                ? (int)Math.Round(segundos, MidpointRounding.AwayFromZero)
                : null;
        }

        private sealed record RespostaRoutes(List<RotaRoutes>? Routes);
        private sealed record RotaRoutes(int DistanceMeters, string? Duration, PolylineRoutes? Polyline);
        private sealed record PolylineRoutes(string? EncodedPolyline);
    }
}
