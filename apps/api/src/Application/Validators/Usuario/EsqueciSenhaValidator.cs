using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;

namespace Frota360.Application.Validators.Usuario
{
    public class EsqueciSenhaValidator : AbstractValidator<EsqueciSenhaRequest>
    {
        public EsqueciSenhaValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("E-mail inválido.");
        }
    }
}
