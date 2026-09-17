namespace Frota360.Domain.Common
{
    /// <summary>
    /// Chave de servidor da Google Routes API, usada no cálculo de distância e duração de rota.
    ///
    /// Fica no Domain, e não no Application, porque quem a consome é a implementação na
    /// Infrastructure — que referencia apenas o Domain.
    ///
    /// Vem de <c>GoogleRoutes:ApiKey</c>: em desenvolvimento por
    /// <c>dotnet user-secrets set "GoogleRoutes:ApiKey" &lt;valor&gt; --project src/Api</c>;
    /// em produção pela variável de ambiente <c>GoogleRoutes__ApiKey</c> do .env da instância.
    ///
    /// É segredo de servidor: não tem equivalente <c>VITE_</c> e nunca chega ao browser. A chave
    /// do front (Maps JavaScript + Places) é outra, restrita por referenciador HTTP.
    /// </summary>
    public sealed record GoogleRoutesSettings(string ApiKey);
}
