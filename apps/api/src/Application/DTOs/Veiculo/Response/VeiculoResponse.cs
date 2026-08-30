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

        /// <summary>
        /// Derivado na leitura, como <c>Atrasada</c> na manutenção: existe rota aberta com
        /// este veículo. Não é coluna — o estado vive na tabela <c>Rota</c>, e persistir uma
        /// cópia aqui daria um "em rota" envelhecido na primeira rota encerrada fora do fluxo.
        /// </summary>
        public bool EmRota { get; set; }
    }
}
