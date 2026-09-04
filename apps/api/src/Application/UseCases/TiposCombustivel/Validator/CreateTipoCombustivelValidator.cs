using FluentValidation;
using Frota360.Application.DTOs.TipoCombustivel.Request;

namespace Frota360.Application.UseCases.TiposCombustivel.Validator
{
    public class CreateTipoCombustivelValidator : AbstractValidator<CreateTipoCombustivelRequest>
    {
        public CreateTipoCombustivelValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");
        }
    }
}
