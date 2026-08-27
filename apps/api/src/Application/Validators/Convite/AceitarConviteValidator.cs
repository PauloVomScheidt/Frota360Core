using FluentValidation;
using Frota360.Application.DTOs.Convite.Request;

namespace Frota360.Application.Validators.Convite
{
    public class AceitarConviteValidator : AbstractValidator<AceitarConviteRequest>
    {
        public AceitarConviteValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("O token do convite é obrigatório.");

            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("Senha é obrigatória.")
                .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres.")
                .Matches("[A-Z]").WithMessage("Senha deve ter ao menos uma letra maiúscula.")
                .Matches("[0-9]").WithMessage("Senha deve ter ao menos um número.");
        }
    }
}
