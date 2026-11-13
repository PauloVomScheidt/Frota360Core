namespace Frota360.Domain.Entities
{
    public class Rota
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }

        /// <summary>
        /// Usuário com a role Motorista a quem a rota pertence. O nome do campo é do
        /// domínio, não da tabela: o motorista é um <see cref="Usuario"/>.
        /// </summary>
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

        // ----- Traçado calculado pela Google Routes API -----
        //
        // DataCalculoRota é o DISCRIMINADOR do bloco: enquanto for nula, nada aqui foi
        // calculado e os demais campos estão nos seus zeros de criação. Os valores são
        // não-anuláveis por definição do modelo, então 0 metros e "não calculado" seriam
        // indistinguíveis sem ela — nenhuma leitura pode olhar distância, duração ou
        // coordenada sem antes checar esta data.
        //
        // Endereço e coordenada convivem de propósito: o texto é o que o usuário escolheu
        // no Places e continua legível quando o traçado envelhece; a coordenada é o que foi
        // efetivamente enviado ao cálculo. O endereço é obrigatório — substituiu os antigos
        // Origem/Destino; a coordenada só chega quando alguém calcula o trajeto.
        public string EnderecoPartida { get; set; } = string.Empty;
        public decimal LatitudePartida { get; set; }
        public decimal LongitudePartida { get; set; }

        public string EnderecoChegada { get; set; } = string.Empty;
        public decimal LatitudeChegada { get; set; }
        public decimal LongitudeChegada { get; set; }

        public int DistanciaMetros { get; set; }
        public int DuracaoEstimadaSegundos { get; set; }

        /// <summary>Polyline codificada do traçado, como devolvida pela Routes API.</summary>
        public string? PolylineCodificada { get; set; }

        /// <summary>Quando o traçado foi calculado. Nula = rota sem cálculo.</summary>
        public DateTime? DataCalculoRota { get; set; }

        // Navegação. O motorista é carregado por Include para desnormalizar o nome na
        // resposta — sem isso, uma rota de quem foi rebaixado perderia a identificação.
        public Usuario? Motorista { get; set; }
        public Veiculo? Veiculo { get; set; }
    }
}
