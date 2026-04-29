namespace Frota360.Application.DTOs.Veiculo.Request
{
    public class CreateVeiculoRequest
    {
        public string NomeVeiculo { get; set; } = string.Empty;
        public string MarcaVeiculo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public int Quilometragem { get; set; }
        public string? UltimoMotorista { get; set; }
        public DateTime? DataUltimaViagem { get; set; }
    }
}
