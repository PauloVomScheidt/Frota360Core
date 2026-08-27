using FluentValidation;
using Frota360.Application.DTOs.TipoManutencao.Request;

namespace Frota360.Application.UseCases.TiposManutencao.Validator
{
    public class UpdateTipoManutencaoValidator : AbstractValidator<UpdateTipoManutencaoRequest>
    {
        public UpdateTipoManutencaoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.IntervaloKm)
                .GreaterThan(0).WithMessage("Intervalo em km deve ser maior que zero.")
                .When(x => x.IntervaloKm.HasValue);
        }
    }
}
