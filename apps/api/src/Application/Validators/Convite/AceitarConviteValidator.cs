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

            // CPF e nascimento são opcionais — mas, se vierem, vêm certos. Sem a regra dos
            // 18 anos do antigo cadastro de motorista: o aceite serve a todas as roles.
            RuleFor(x => x.CPF)
                .Length(11).WithMessage("CPF deve ter exatamente 11 dígitos.")
                .Matches(@"^\d{11}$").WithMessage("CPF deve conter apenas números.")
                .Must(cpf => ValidatorHelpers.IsValidCPF(cpf!)).WithMessage("CPF inválido.")
                .When(x => !string.IsNullOrWhiteSpace(x.CPF));

            RuleFor(x => x.DataNascimento)
                .LessThan(DateTime.Today).WithMessage("Data de nascimento não pode ser futura.")
                .GreaterThan(DateTime.Today.AddYears(-100)).WithMessage("Data de nascimento inválida.")
                .When(x => x.DataNascimento is not null);
        }
    }
}
