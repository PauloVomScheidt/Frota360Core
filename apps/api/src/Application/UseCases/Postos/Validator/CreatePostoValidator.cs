using FluentValidation;
using Frota360.Application.DTOs.Posto.Request;

namespace Frota360.Application.UseCases.Postos.Validator
{
    public class CreatePostoValidator : AbstractValidator<CreatePostoRequest>
    {
        public CreatePostoValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            // Só a forma: o credenciamento não exige CNPJ, e validar dígito verificador
            // recusaria posto de fronteira e frota própria.
            RuleFor(x => x.Cnpj)
                .MaximumLength(18).WithMessage("CNPJ deve ter no máximo 18 caracteres.");

            RuleFor(x => x.Cidade)
                .MaximumLength(100).WithMessage("Cidade deve ter no máximo 100 caracteres.");
        }
    }
}
