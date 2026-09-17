using Frota360.Application.Common;

namespace Frota360.Application.DTOs.Rota.Request
{
    public class CreateRotaRequest : IRequestComTrajeto
    {
        public string EnderecoPartida { get; set; } = string.Empty;
        public string EnderecoChegada { get; set; } = string.Empty;

        /// <summary>
        /// Coordenadas do Places. Opcionais enquanto o autocomplete não existe na tela:
        /// exigi-las obrigaria a digitar latitude e longitude à mão. Ausentes, a rota é
        /// gravada com zero e fica sem traçado — <c>DataCalculoRota</c> segue nula.
        /// </summary>
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

        /// <summary>Odômetro do veículo na abertura da rota.</summary>
        public int KmInicial { get; set; }
    }
}
