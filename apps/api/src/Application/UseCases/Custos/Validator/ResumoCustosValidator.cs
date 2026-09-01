using FluentValidation;
using Frota360.Application.DTOs.Custo.Request;

namespace Frota360.Application.UseCases.Custos.Validator
{
    public class ResumoCustosValidator : AbstractValidator<ResumoCustosRequest>
    {
        public ResumoCustosValidator()
        {
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
}
