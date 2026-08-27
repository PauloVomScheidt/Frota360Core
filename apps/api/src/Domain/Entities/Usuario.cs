namespace Frota360.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiraEm { get; set; }
        public string? ResetSenhaTokenHash { get; set; }
        public DateTime? ResetSenhaExpiraEm { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
