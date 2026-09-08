using FluentValidation;
using Frota360.Application.DTOs.Abastecimento.Request;

namespace Frota360.Application.UseCases.Abastecimentos.Validator
{
    public class CreateAbastecimentoValidator : AbstractValidator<CreateAbastecimentoRequest>
    {
        public CreateAbastecimentoValidator()
        {
            RuleFor(x => x.VeiculoId).GreaterThan(0).WithMessage("Veículo é obrigatório.");

            // Só a forma: se o motorista é obrigatório depende do papel de quem lança, e
            // isso o validator não sabe — a regra vive no handler.
            RuleFor(x => x.MotoristaId)
                .GreaterThan(0).WithMessage("Motorista inválido.")
                .When(x => x.MotoristaId.HasValue);

            RuleFor(x => x.TipoCombustivelId).GreaterThan(0).WithMessage("Tipo de combustível é obrigatório.");
            RuleFor(x => x.PostoId).GreaterThan(0).WithMessage("Posto é obrigatório.");

            RuleFor(x => x.Litros)
                .GreaterThan(0).WithMessage("Volume em litros deve ser maior que zero.")
                .LessThanOrEqualTo(2_000).WithMessage("Volume em litros parece inválido.");

            RuleFor(x => x.ValorLitro)
                .GreaterThan(0).WithMessage("Valor do litro deve ser maior que zero.")
                .LessThanOrEqualTo(100).WithMessage("Valor do litro parece inválido.");

            // O valor total não entra: o servidor o calcula a partir dos dois campos acima.
            RuleFor(x => x.Odometro)
                .GreaterThan(0).WithMessage("Odômetro é obrigatório.")
                .LessThanOrEqualTo(10_000_000).WithMessage("Odômetro parece inválido.");

            RuleFor(x => x.NotaFiscal)
                .NotEmpty().WithMessage("Número da nota fiscal é obrigatório.")
                .MaximumLength(30).WithMessage("Número da nota fiscal deve ter no máximo 30 caracteres.");

            RuleFor(x => x.Frentista)
                .MaximumLength(100).WithMessage("Nome do frentista deve ter no máximo 100 caracteres.");

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
