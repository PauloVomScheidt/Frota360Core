namespace Frota360.Application.DTOs.Usuario.Response
{
    public static class AuthResponseMappings
    {
        /// <summary>Descarta token e refresh token — usado pelo controller depois de movê-los para o cookie.</summary>
        public static SessaoResponse ToSessaoResponse(this AuthResponse auth) => new()
        {
            Nome = auth.Nome,
            Email = auth.Email,
            Role = auth.Role,
        };
    }
}
