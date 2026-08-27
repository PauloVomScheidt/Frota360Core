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

        /// <summary>Odômetro do veículo na abertura da rota.</summary>
        public int KmInicial { get; set; }

        // Preenchidos no encerramento
        public int? KmFinal { get; set; }

        /// <summary>
        /// Vem de KmFinal - KmInicial, mas é persistido: diferente de "atrasada" na
        /// manutenção, este é um fato histórico da rota — não muda depois de gravado
        /// e não depende do estado atual do veículo.
        /// </summary>
        public int? KmPercorrido { get; set; }

        // Navegação
        public Motorista? Motorista { get; set; }
        public Veiculo? Veiculo { get; set; }
    }
}
