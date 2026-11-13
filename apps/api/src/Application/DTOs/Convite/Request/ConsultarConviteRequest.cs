namespace Frota360.Application.DTOs.Convite.Request
{
    /// <summary>
    /// O token vai no corpo, e não na query string: URL entra em log de proxy e de acesso,
    /// e o token do convite é a credencial de quem ainda não tem senha.
    /// </summary>
    public class ConsultarConviteRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
