using FluentValidation;
using Frota360.Application.DTOs.Posto.Request;

namespace Frota360.Application.UseCases.Postos.Validator
{
    public class UpdatePostoValidator : AbstractValidator<UpdatePostoRequest>
    {
        public UpdatePostoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Cnpj)
                .MaximumLength(18).WithMessage("CNPJ deve ter no máximo 18 caracteres.");

            RuleFor(x => x.Cidade)
                .MaximumLength(100).WithMessage("Cidade deve ter no máximo 100 caracteres.");
        }
    }
}
