using FluentValidation;
using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.Validators;

namespace Frota360.Application.UseCases.Motoristas.Validator
{
    public class CreateMotoristaValidator : AbstractValidator<CreateMotoristaRequest>
    {
        public CreateMotoristaValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("E-mail inválido.")
                .MaximumLength(150).WithMessage("E-mail deve ter no máximo 150 caracteres.");

            RuleFor(x => x.CPF)
                .NotEmpty().WithMessage("CPF é obrigatório.")
                .Length(11).WithMessage("CPF deve ter exatamente 11 dígitos.")
                .Matches(@"^\d{11}$").WithMessage("CPF deve conter apenas números.")
                .Must(ValidatorHelpers.IsValidCPF).WithMessage("CPF inválido.");

            RuleFor(x => x.DataNascimento)
                .NotEmpty().WithMessage("Data de nascimento é obrigatória.")
                .LessThan(DateTime.Today).WithMessage("Data de nascimento não pode ser futura.")
                .GreaterThan(DateTime.Today.AddYears(-100)).WithMessage("Data de nascimento inválida.")
                .Must(ValidatorHelpers.Is18YearsOld).WithMessage("Motorista deve ter pelo menos 18 anos.");
        }
    }
}
