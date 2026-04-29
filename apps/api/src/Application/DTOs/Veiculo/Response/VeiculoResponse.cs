namespace Frota360.Application.DTOs.Veiculo.Response
{
    public class VeiculoResponse
    {
        public int Id { get; set; }
        public string NomeVeiculo { get; set; } = string.Empty;
        public string MarcaVeiculo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public int Quilometragem { get; set; }
        public string? UltimoMotorista { get; set; }
        public DateTime? DataUltimaViagem { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
