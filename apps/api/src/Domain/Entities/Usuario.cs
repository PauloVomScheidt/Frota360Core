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

        /// <summary>
        /// Dados pessoais opcionais, preenchidos pela própria pessoa no aceite do
        /// convite. Nasceram do cadastro de motorista, mas valem para qualquer role.
        /// </summary>
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }

        public bool Ativo { get; set; } = true;
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiraEm { get; set; }
        public string? ResetSenhaTokenHash { get; set; }
        public DateTime? ResetSenhaExpiraEm { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
