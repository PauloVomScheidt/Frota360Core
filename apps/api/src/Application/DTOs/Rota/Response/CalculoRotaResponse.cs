namespace Frota360.Application.DTOs.Rota.Response
{
    /// <summary>Trajeto calculado pela Routes API, como devolvido ao cliente.</summary>
    public class CalculoRotaResponse
    {
        public int DistanciaMetros { get; set; }
        public int DuracaoEstimadaSegundos { get; set; }

        /// <summary>
        /// Nula quando a Google devolve o trajeto sem o traçado — a distância e a duração
        /// continuam válidas, só não há linha para desenhar no mapa.
        /// </summary>
        public string? PolylineCodificada { get; set; }
    }
}
