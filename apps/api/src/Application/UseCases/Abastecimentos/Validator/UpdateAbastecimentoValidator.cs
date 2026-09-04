using FluentValidation;
using Frota360.Application.DTOs.Abastecimento.Request;

namespace Frota360.Application.UseCases.Abastecimentos.Validator
{
    public class UpdateAbastecimentoValidator : AbstractValidator<UpdateAbastecimentoRequest>
    {
        public UpdateAbastecimentoValidator()
        {
            RuleFor(x => x.TipoCombustivelId).GreaterThan(0).WithMessage("Tipo de combustível é obrigatório.");
            RuleFor(x => x.PostoId).GreaterThan(0).WithMessage("Posto é obrigatório.");

            RuleFor(x => x.Litros)
                .GreaterThan(0).WithMessage("Volume em litros deve ser maior que zero.")
                .LessThanOrEqualTo(2_000).WithMessage("Volume em litros parece inválido.");

            RuleFor(x => x.ValorLitro)
                .GreaterThan(0).WithMessage("Valor do litro deve ser maior que zero.")
                .LessThanOrEqualTo(100).WithMessage("Valor do litro parece inválido.");

            RuleFor(x => x.Odometro)
                .GreaterThan(0).WithMessage("Odômetro é obrigatório.")
                .LessThanOrEqualTo(10_000_000).WithMessage("Odômetro parece inválido.");

            RuleFor(x => x.NotaFiscal)
                .NotEmpty().WithMessage("Número da nota fiscal é obrigatório.")
                .MaximumLength(30).WithMessage("Número da nota fiscal deve ter no máximo 30 caracteres.");

            RuleFor(x => x.Frentista)
                .MaximumLength(100).WithMessage("Nome do frentista deve ter no máximo 100 caracteres.");

            RuleFor(x => x.DataAbastecimento)
                .NotEmpty().WithMessage("Data do abastecimento é obrigatória.")
                .LessThanOrEqualTo(_ => DateTime.Now.Date.AddDays(1))
                .WithMessage("Data do abastecimento não pode estar no futuro.");

            RuleFor(x => x.Observacao)
                .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
        }
    }
}
