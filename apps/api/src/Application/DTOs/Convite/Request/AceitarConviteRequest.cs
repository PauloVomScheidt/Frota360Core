namespace Frota360.Application.DTOs.Convite.Request
{
    public class AceitarConviteRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;

        /// <summary>
        /// Dados pessoais opcionais, informados pela própria pessoa. Só os 11 dígitos
        /// do CPF — a máscara é coisa da interface.
        /// </summary>
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }
    }
}
