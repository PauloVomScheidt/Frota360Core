using FluentValidation;
using Frota360.Application.DTOs.Despesa.Request;

namespace Frota360.Application.UseCases.Despesas.Validator
{
    public class UpdateDespesaValidator : AbstractValidator<UpdateDespesaRequest>
    {
        public UpdateDespesaValidator()
        {
            RuleFor(x => x.VeiculoId)
                .GreaterThan(0).WithMessage("Veículo é obrigatório.");

            RuleFor(x => x.TipoDespesaId)
                .GreaterThan(0).WithMessage("Tipo de despesa é obrigatório.");

            RuleFor(x => x.MotoristaId)
                .GreaterThan(0).WithMessage("Motorista inválido.")
                .When(x => x.MotoristaId.HasValue);

            RuleFor(x => x.Valor)
                .GreaterThan(0).WithMessage("Valor deve ser maior que zero.")
                .LessThanOrEqualTo(1_000_000).WithMessage("Valor deve ser no máximo R$ 1.000.000,00.");

            RuleFor(x => x.DataDespesa)
                .NotEmpty().WithMessage("Data da despesa é obrigatória.")
                .LessThanOrEqualTo(_ => DateTime.Now.Date.AddDays(1))
                .WithMessage("A data da despesa não pode ser futura.");

            RuleFor(x => x.Observacao)
                .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
        }
    }
}
