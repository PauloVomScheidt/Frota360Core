namespace Frota360.Application.DTOs.Rota.Response
{
    public class RotaResponse
    {
        public int Id { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public int CodigoMotorista { get; set; }
        public int CodigoVeiculo { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public DateTime DataInclusao { get; set; }
        public int KmInicial { get; set; }
        public int? KmFinal { get; set; }
        public int? KmPercorrido { get; set; }
    }
}
