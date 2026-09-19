using FluentValidation;

namespace Frota360.Application.Common
{
    /// <summary>
    /// Request que carrega o traçado já calculado por <c>POST /rota/calcular</c>. Existir como
    /// interface é o que permite validar o bloco num lugar só, em vez de repetir as regras no
    /// create e no update — mesmo arranjo de <see cref="IRequestPaginado"/>.
    /// </summary>
    public interface IRequestComTrajeto
    {
        int? DistanciaMetros { get; set; }
        int? DuracaoEstimadaSegundos { get; set; }
        string? PolylineCodificada { get; set; }
    }

    public static class RegrasDeTrajeto
    {
        /// <summary>
        /// Faixa de velocidade média aceita para um trajeto rodoviário. O piso barra o par que
        /// diria "400 km em 20 minutos" ao contrário — distância grande com duração absurda —,
        /// e o teto barra a distância inflada. São limites folgados de propósito: a checagem
        /// existe para pegar valor forjado ou corrompido, não para auditar o Google.
        /// </summary>
        public const int VelocidadeMinimaKmH = 5;
        public const int VelocidadeMaximaKmH = 180;

        /// <summary>A coluna é <c>text</c>, sem teto no banco; o limite aqui é contra payload abusivo.</summary>
        public const int TamanhoMaximoPolyline = 20_000;

        /// <summary>
        /// Distância e duração chegam do cliente, e não do servidor: o front já pagou pela
        /// chamada à Routes API e reenviá-la no salvamento custaria uma segunda cobrança. A
        /// contrapartida é esta trava — sem ela, um corpo manipulado gravaria qualquer número.
        /// </summary>
        public static bool VelocidadeMediaPlausivel(int? distanciaMetros, int? duracaoSegundos)
        {
            // A regra só vale para o par completo: rota salva sem traçado é caso legítimo.
            if (distanciaMetros is null || duracaoSegundos is null)
                return true;

            // Zero é o valor de "sem traçado", não uma medida a julgar. Negativo já é recusado
            // pelas regras de cada campo.
            if (distanciaMetros <= 0)
                return true;

            // Distância com duração zerada seria velocidade infinita.
            if (duracaoSegundos <= 0)
                return false;

            var velocidadeKmH = distanciaMetros.Value * 3.6 / duracaoSegundos.Value;
            return velocidadeKmH >= VelocidadeMinimaKmH && velocidadeKmH <= VelocidadeMaximaKmH;
        }

        /// <summary>Chame no construtor do validator que recebe o traçado.</summary>
        public static void AplicarRegrasDeTrajeto<T>(this AbstractValidator<T> validator)
            where T : IRequestComTrajeto
        {
            // ⚠️ É `OverridePropertyName`, e não `WithName`: o controller monta o erro como
            // "{PropertyName}: {ErrorMessage}" a partir de `ValidationFailure.PropertyName`, e
            // o `WithName` mexe só no placeholder de dentro da mensagem. Sem isto o usuário lê
            // "DuracaoEstimadaSegundos: ..." — nome interno de campo numa mensagem de tela.
            validator.RuleFor(x => x.DistanciaMetros)
                .GreaterThanOrEqualTo(0).WithMessage("Distância não pode ser negativa.")
                .When(x => x.DistanciaMetros is not null)
                .OverridePropertyName("Distância");

            validator.RuleFor(x => x.DuracaoEstimadaSegundos)
                .GreaterThanOrEqualTo(0).WithMessage("Duração não pode ser negativa.")
                .When(x => x.DuracaoEstimadaSegundos is not null)
                .OverridePropertyName("Duração");

            validator.RuleFor(x => x.PolylineCodificada)
                .MaximumLength(TamanhoMaximoPolyline).WithMessage("Traçado da rota excede o tamanho aceito.")
                .OverridePropertyName("Trajeto");

            // A mensagem é deliberadamente genérica: quem digita não tem o que fazer com
            // "velocidade média de 1.200 km/h", e o número é detalhe da nossa checagem.
            validator.RuleFor(x => x.DuracaoEstimadaSegundos)
                .Must((requisicao, _) =>
                    VelocidadeMediaPlausivel(requisicao.DistanciaMetros, requisicao.DuracaoEstimadaSegundos))
                .WithMessage("A distância e a duração informadas não são compatíveis entre si. Refaça o cálculo da rota.")
                .OverridePropertyName("Trajeto");
        }
    }
}
