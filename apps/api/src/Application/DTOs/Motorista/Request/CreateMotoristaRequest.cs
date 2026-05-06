namespace Frota360.Application.DTOs.Motorista.Request
{
    public class CreateMotoristaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
    }
}
