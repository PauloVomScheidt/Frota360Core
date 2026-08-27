using FluentValidation;
using Frota360.Application.DTOs.Manutencao.Request;

namespace Frota360.Application.UseCases.Manutencoes.Validator
{
    public class ConcluirManutencaoValidator : AbstractValidator<ConcluirManutencaoRequest>
    {
        public ConcluirManutencaoValidator()
        {
            RuleFor(x => x.QuilometragemRealizada)
                .GreaterThan(0).WithMessage("Quilometragem realizada é obrigatória.")
                .LessThanOrEqualTo(2_000_000).WithMessage("Quilometragem realizada parece inválida.");

            // Margem de um dia sobre o UTC: o operador lança no fuso dele e não pode
            // ser barrado por lançar "hoje" antes de o UTC virar.
            RuleFor(x => x.DataRealizacao)
                .NotEmpty().WithMessage("Data de realização é obrigatória.")
                .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Data de realização não pode estar no futuro.");

            RuleFor(x => x.Custo)
                .GreaterThanOrEqualTo(0).WithMessage("Custo não pode ser negativo.")
                .When(x => x.Custo.HasValue);

            RuleFor(x => x.Observacao)
                .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
        }
    }
}
