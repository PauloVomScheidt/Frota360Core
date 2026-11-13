using FluentValidation;
using Frota360.Application.DTOs.TipoDespesa.Request;

namespace Frota360.Application.UseCases.TiposDespesa.Validator
{
    public class CreateTipoDespesaValidator : AbstractValidator<CreateTipoDespesaRequest>
    {
        public CreateTipoDespesaValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");
        }
    }
}
