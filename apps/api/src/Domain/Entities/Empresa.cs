namespace Frota360.Domain.Entities
{
    public class Empresa
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? CNPJ { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataInclusao { get; set; }
    }
}
