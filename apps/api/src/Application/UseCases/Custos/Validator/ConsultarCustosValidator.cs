using FluentValidation;
using Frota360.Application.DTOs.Custo.Request;
using Frota360.Domain.Enums;

namespace Frota360.Application.UseCases.Custos.Validator
{
    public class ConsultarCustosValidator : AbstractValidator<ConsultarCustosRequest>
    {
        /// <summary>
        /// Teto do que a API entrega de uma vez. Sem ele, um <c>tamanhoPagina=999999</c>
        /// materializa o histórico inteiro da empresa em memória.
        /// </summary>
        private const int TamanhoMaximoPagina = 100;

        public ConsultarCustosValidator()
        {
            RuleFor(x => x.Pagina)
                .GreaterThan(0).WithMessage("Página deve ser maior que zero.");

            RuleFor(x => x.TamanhoPagina)
                .InclusiveBetween(1, TamanhoMaximoPagina)
                .WithMessage($"Tamanho da página deve ficar entre 1 e {TamanhoMaximoPagina}.");

            RuleFor(x => x.Origem)
                .Must(OrigensDeCusto.EhValida)
                .WithMessage(OrigensDeCusto.MensagemDeErro)
                .When(x => !string.IsNullOrWhiteSpace(x.Origem));

            RuleFor(x => x.Ate)
                .GreaterThanOrEqualTo(x => x.De!.Value)
                .WithMessage("A data final não pode ser anterior à inicial.")
                .When(x => x.De.HasValue && x.Ate.HasValue);
        }
    }

    /// <summary>
    /// Compartilhado pelos dois validators de custo — a origem é o mesmo vocabulário fechado
    /// na lista e no resumo.
    /// </summary>
    internal static class OrigensDeCusto
    {
        internal static readonly string MensagemDeErro =
            $"Origem inválida. Valores aceitos: {string.Join(", ", Enum.GetNames<OrigemCusto>())}.";

        internal static bool EhValida(string? origem)
            => Enum.TryParse<OrigemCusto>(origem, ignoreCase: true, out _);
    }
}
