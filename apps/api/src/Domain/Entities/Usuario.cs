namespace Frota360.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiraEm { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
