using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;

namespace Frota360.Application.Validators.Usuario
{
    public class RedefinirSenhaValidator : AbstractValidator<RedefinirSenhaRequest>
    {
        public RedefinirSenhaValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("O token de redefinição é obrigatório.");

            RuleFor(x => x.NovaSenha)
                .NotEmpty().WithMessage("Senha é obrigatória.")
                .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres.")
                .Matches("[A-Z]").WithMessage("Senha deve ter ao menos uma letra maiúscula.")
                .Matches("[0-9]").WithMessage("Senha deve ter ao menos um número.");
        }
    }
}
