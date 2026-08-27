using FluentValidation;
using Frota360.Application.DTOs.Manutencao.Request;

namespace Frota360.Application.UseCases.Manutencoes.Validator
{
    public class UpdateManutencaoValidator : AbstractValidator<UpdateManutencaoRequest>
    {
        public UpdateManutencaoValidator()
        {
            RuleFor(x => x.VeiculoId)
                .GreaterThan(0).WithMessage("Veículo é obrigatório.");

            RuleFor(x => x.TipoManutencaoId)
                .GreaterThan(0).WithMessage("Tipo de manutenção é obrigatório.");

            RuleFor(x => x.QuilometragemPrevista)
                .GreaterThan(0).WithMessage("Quilometragem prevista é obrigatória.")
                .LessThanOrEqualTo(2_000_000).WithMessage("Quilometragem prevista parece inválida.");

            RuleFor(x => x.Observacao)
                .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
        }
    }
}
