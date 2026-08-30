using FluentValidation;
using Frota360.Application.DTOs.Manutencao.Request;

namespace Frota360.Application.UseCases.Manutencoes.Validator
{
    public class CreateManutencaoValidator : AbstractValidator<CreateManutencaoRequest>
    {
        public CreateManutencaoValidator()
        {
            RuleFor(x => x.VeiculoId)
                .GreaterThan(0).WithMessage("Veículo é obrigatório.");

            RuleFor(x => x.TipoManutencaoId)
                .GreaterThan(0).WithMessage("Tipo de manutenção é obrigatório.");

            RuleFor(x => x.QuilometragemPrevista)
                .GreaterThan(0).WithMessage("Quilometragem prevista é obrigatória.")
                .LessThanOrEqualTo(2_000_000).WithMessage("Quilometragem prevista parece inválida.");

            // Data é o prazo alternativo ao km; quando informada, precisa ser futura.
            RuleFor(x => x.DataPrevista)
                .GreaterThanOrEqualTo(_ => DateTime.Now.Date)
                .WithMessage("Data prevista não pode estar no passado.")
                .When(x => x.DataPrevista.HasValue);

            RuleFor(x => x.Observacao)
                .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
        }
    }
}
