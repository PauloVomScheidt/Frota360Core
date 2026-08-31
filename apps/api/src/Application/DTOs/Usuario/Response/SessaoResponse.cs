namespace Frota360.Application.DTOs.Usuario.Response
{
    /// <summary>
    /// O que o front recebe no corpo de login/refresh/aceite de convite — só identidade.
    /// Token e refresh token nunca saem daqui: viajam em cookie HttpOnly, emitido pela Api
    /// a partir do <c>AuthResponse</c> interno, fora do alcance de JavaScript.
    /// </summary>
    public class SessaoResponse
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
