namespace Frota360.Domain.Entities
{
    public class Rota
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public int CodigoMotorista { get; set; }
        public int CodigoVeiculo { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public DateTime DataInclusao { get; set; }

        // Navegação
        public Motorista? Motorista { get; set; }
        public Veiculo? Veiculo { get; set; }
    }
}
