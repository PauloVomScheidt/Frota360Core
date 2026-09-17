namespace Frota360.Application.DTOs.Rota.Request
{
    /// <summary>
    /// Coordenadas de origem e destino para o cálculo de trajeto. Não carrega id de rota:
    /// o cálculo é uma consulta avulsa, feita antes de a rota existir.
    /// </summary>
    public class CalcularRotaRequest
    {
        public decimal LatitudeOrigem { get; set; }
        public decimal LongitudeOrigem { get; set; }
        public decimal LatitudeDestino { get; set; }
        public decimal LongitudeDestino { get; set; }
    }
}
