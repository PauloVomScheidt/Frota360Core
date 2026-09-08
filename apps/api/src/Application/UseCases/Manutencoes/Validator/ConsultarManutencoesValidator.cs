using FluentValidation;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Manutencao.Request;

namespace Frota360.Application.UseCases.Manutencoes.Validator
{
    public class ConsultarManutencoesValidator : AbstractValidator<ConsultarManutencoesRequest>
    {
        public ConsultarManutencoesValidator()
        {
            this.AplicarRegrasDePaginacao();

            RuleFor(x => x.Ate)
                .GreaterThanOrEqualTo(x => x.De!.Value)
                .WithMessage("A data final não pode ser anterior à inicial.")
                .When(x => x.De.HasValue && x.Ate.HasValue);
        }
    }
}
