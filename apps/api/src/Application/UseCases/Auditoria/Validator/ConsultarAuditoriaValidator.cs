using FluentValidation;
using Frota360.Application.DTOs.Auditoria.Request;
using Frota360.Domain.Common;

namespace Frota360.Application.UseCases.Auditoria.Validator
{
    public class ConsultarAuditoriaValidator : AbstractValidator<ConsultarAuditoriaRequest>
    {
        /// <summary>
        /// Teto do que a API entrega de uma vez. Sem ele, um <c>tamanhoPagina=999999</c>
        /// materializa a trilha inteira da empresa em memória.
        /// </summary>
        private const int TamanhoMaximoPagina = 100;

        public ConsultarAuditoriaValidator()
        {
            RuleFor(x => x.Pagina)
                .GreaterThan(0).WithMessage("Página deve ser maior que zero.");

            RuleFor(x => x.TamanhoPagina)
                .InclusiveBetween(1, TamanhoMaximoPagina)
                .WithMessage($"Tamanho da página deve ficar entre 1 e {TamanhoMaximoPagina}.");

            RuleFor(x => x.Entidade)
                .Must(e => EntidadesAuditadas.Todas.Contains(e!))
                .WithMessage($"Entidade inválida. Valores aceitos: {string.Join(", ", EntidadesAuditadas.Todas)}.")
                .When(x => !string.IsNullOrWhiteSpace(x.Entidade));

            RuleFor(x => x.Acao)
                .Must(a => AcoesAuditoria.Todas.Contains(a!))
                .WithMessage($"Ação inválida. Valores aceitos: {string.Join(", ", AcoesAuditoria.Todas)}.")
                .When(x => !string.IsNullOrWhiteSpace(x.Acao));

            RuleFor(x => x.Ate)
                .GreaterThanOrEqualTo(x => x.De!.Value)
                .WithMessage("A data final não pode ser anterior à inicial.")
                .When(x => x.De.HasValue && x.Ate.HasValue);
        }
    }
}
