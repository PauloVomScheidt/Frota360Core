using FluentValidation;
using Frota360.Application.DTOs.Abastecimento.Request;

namespace Frota360.Application.UseCases.Abastecimentos.Validator
{
    public class UpdateAbastecimentoValidator : AbstractValidator<UpdateAbastecimentoRequest>
    {
        public UpdateAbastecimentoValidator()
        {
            RuleFor(x => x.Valor)
                .GreaterThan(0).WithMessage("Valor deve ser maior que zero.")
                .LessThanOrEqualTo(100_000).WithMessage("Valor parece inválido.");

            RuleFor(x => x.DataAbastecimento)
                .NotEmpty().WithMessage("Data do abastecimento é obrigatória.")
                .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Data do abastecimento não pode estar no futuro.");

            RuleFor(x => x.Observacao)
                .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
        }
    }
}
