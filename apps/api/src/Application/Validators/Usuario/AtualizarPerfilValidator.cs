using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;

namespace Frota360.Application.Validators.Usuario
{
    /// <summary>
    /// Mesmas regras do aceite de convite (<c>AceitarConviteValidator</c>): é o outro caminho
    /// pelo qual CPF e nascimento entram no sistema, e os dois precisam concordar — senão um
    /// dado aceito num deles seria recusado no outro.
    /// </summary>
    public class AtualizarPerfilValidator : AbstractValidator<AtualizarPerfilRequest>
    {
        public AtualizarPerfilValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

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
