using FluentValidation;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Abastecimento.Request;

namespace Frota360.Application.UseCases.Abastecimentos.Validator
{
    /// <summary>
    /// Vale para a listagem e para o <c>/resumo</c>, que compartilham o request.
    /// </summary>
    public class ConsultarAbastecimentosValidator : AbstractValidator<ConsultarAbastecimentosRequest>
    {
        public ConsultarAbastecimentosValidator()
        {
            this.AplicarRegrasDePaginacao();

            RuleFor(x => x.Ate)
                .GreaterThanOrEqualTo(x => x.De!.Value)
                .WithMessage("A data final não pode ser anterior à inicial.")
                .When(x => x.De.HasValue && x.Ate.HasValue);
        }
    }
}
