using Frota360.Application.Common;

namespace Frota360.Application.DTOs.Rota.Request
{
    /// <summary>
    /// Edição dos dados de planejamento da rota. Ativo e DataFim não entram aqui:
    /// encerrar é a única transição de estado, e passa por POST /rota/{id}/encerrar.
    /// </summary>
    public class UpdateRotaRequest : IRequestComTrajeto
    {
        public string EnderecoPartida { get; set; } = string.Empty;
        public string EnderecoChegada { get; set; } = string.Empty;

        /// <summary>Opcionais, como no create — ver <see cref="CreateRotaRequest"/>.</summary>
        public decimal? LatitudePartida { get; set; }
        public decimal? LongitudePartida { get; set; }
        public decimal? LatitudeChegada { get; set; }
        public decimal? LongitudeChegada { get; set; }

        /// <summary>
        /// Traçado que o cliente já calculou por <c>POST /rota/calcular</c>. Vem do cliente
        /// em vez de ser recalculado aqui porque a chamada à Routes API é cobrada, e o front
        /// acabou de pagar por este mesmo trajeto. Não é dado financeiro: o R$/km sai do
        /// odômetro (<c>KmPercorrido</c>), não daqui.
        ///
        /// <c>DataCalculoRota</c> não entra: quem a carimba é o servidor, e só quando a
        /// distância de fato chega.
        /// </summary>
        public int? DistanciaMetros { get; set; }
        public int? DuracaoEstimadaSegundos { get; set; }
        public string? PolylineCodificada { get; set; }

        public int CodigoMotorista { get; set; }
        public int CodigoVeiculo { get; set; }
        public DateTime DataInicio { get; set; }
    }
}
