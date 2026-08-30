using FluentValidation;
using Frota360.Application.DTOs.Abastecimento.Request;

namespace Frota360.Application.UseCases.Abastecimentos.Validator
{
    public class CreateAbastecimentoValidator : AbstractValidator<CreateAbastecimentoRequest>
    {
        public CreateAbastecimentoValidator()
        {
            RuleFor(x => x.VeiculoId)
                .GreaterThan(0).WithMessage("Veículo é obrigatório.");

            RuleFor(x => x.Valor)
                .GreaterThan(0).WithMessage("Valor deve ser maior que zero.")
                .LessThanOrEqualTo(100_000).WithMessage("Valor parece inválido.");

            // Só a forma: se o motorista é obrigatório depende do papel de quem lança, e
            // isso o validator não sabe — a regra vive no handler.
            RuleFor(x => x.MotoristaId)
                .GreaterThan(0).WithMessage("Motorista inválido.")
                .When(x => x.MotoristaId.HasValue);

            // Abastecimento é sempre um fato já ocorrido — não se agenda combustível.
            RuleFor(x => x.DataAbastecimento)
                .NotEmpty().WithMessage("Data do abastecimento é obrigatória.")
                .LessThanOrEqualTo(_ => DateTime.Now.Date.AddDays(1))
                .WithMessage("Data do abastecimento não pode estar no futuro.");

            RuleFor(x => x.Observacao)
                .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
        }
    }
}
