namespace Frota360.Application.DTOs.Usuario.Response
{
    public class UsuarioResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
