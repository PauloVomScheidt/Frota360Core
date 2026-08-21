namespace Frota360.Application.DTOs.Convite.Request
{
    public class AceitarConviteRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
