using FluentValidation;
using Frota360.Application.DTOs.Backoffice.Request;

namespace Frota360.Application.Validators.Backoffice
{
    public class ProvisionarEmpresaValidator : AbstractValidator<ProvisionarEmpresaRequest>
    {
        public ProvisionarEmpresaValidator()
        {
            RuleFor(x => x.NomeEmpresa)
                .NotEmpty().WithMessage("Nome da empresa é obrigatório.")
                .MaximumLength(150).WithMessage("Nome da empresa deve ter no máximo 150 caracteres.");

            RuleFor(x => x.CNPJ)
                .Matches(@"^\d{14}$").WithMessage("CNPJ deve conter exatamente 14 dígitos numéricos.")
                .When(x => !string.IsNullOrWhiteSpace(x.CNPJ));

            RuleFor(x => x.EmailAdmin)
                .NotEmpty().WithMessage("E-mail do administrador é obrigatório.")
                .EmailAddress().WithMessage("E-mail inválido.")
                .MaximumLength(150).WithMessage("E-mail deve ter no máximo 150 caracteres.");
        }
    }
}
