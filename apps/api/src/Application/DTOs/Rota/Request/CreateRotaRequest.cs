namespace Frota360.Application.DTOs.Rota.Request
{
    public class CreateRotaRequest
    {
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public int CodigoMotorista { get; set; }
        public int CodigoVeiculo { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }
}
