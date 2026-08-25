namespace Frota360.Domain.Entities
{
    public class Motorista
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
