namespace Frota360.Application.Common
{
    /// <summary>URL base do front-end, usada para montar links enviados por e-mail (convite, reset de senha).</summary>
    public sealed record FrontendSettings(string BaseUrl);
}
