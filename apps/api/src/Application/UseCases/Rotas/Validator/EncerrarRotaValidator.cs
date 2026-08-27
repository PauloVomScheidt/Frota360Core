using FluentValidation;
using Frota360.Application.DTOs.Rota.Request;

namespace Frota360.Application.UseCases.Rotas.Validator
{
    public class EncerrarRotaValidator : AbstractValidator<EncerrarRotaRequest>
    {
        public EncerrarRotaValidator()
        {
            RuleFor(x => x.KmFinal)
                .GreaterThanOrEqualTo(0).WithMessage("Quilometragem final não pode ser negativa.")
                .LessThanOrEqualTo(2_000_000).WithMessage("Quilometragem final parece inválida.");

            // Margem de um dia sobre o UTC: o operador lança no fuso dele e não pode
            // ser barrado por encerrar "hoje" antes de o UTC virar.
            RuleFor(x => x.DataFim)
                .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Data de fim não pode estar no futuro.")
                .When(x => x.DataFim.HasValue);
        }
    }
}
