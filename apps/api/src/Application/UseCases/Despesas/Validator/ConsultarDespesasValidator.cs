using FluentValidation;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Despesa.Request;

namespace Frota360.Application.UseCases.Despesas.Validator
{
    /// <summary>Vale para a listagem e para o <c>/resumo</c>, que compartilham o request.</summary>
    public class ConsultarDespesasValidator : AbstractValidator<ConsultarDespesasRequest>
    {
        public ConsultarDespesasValidator()
        {
            this.AplicarRegrasDePaginacao();

            RuleFor(x => x.Ate)
                .GreaterThanOrEqualTo(x => x.De!.Value)
                .WithMessage("A data final não pode ser anterior à inicial.")
                .When(x => x.De.HasValue && x.Ate.HasValue);
        }
    }
}
