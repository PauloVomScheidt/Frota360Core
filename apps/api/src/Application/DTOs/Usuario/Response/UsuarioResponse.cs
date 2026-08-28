namespace Frota360.Application.DTOs.Usuario.Response
{
    public class UsuarioResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        /// <summary>Opcionais: só existem se a pessoa os informou ao aceitar o convite.</summary>
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
