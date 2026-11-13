using FluentValidation;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Rota.Request;

namespace Frota360.Application.UseCases.Rotas.Validator
{
    public class CalcularRotaValidator : AbstractValidator<CalcularRotaRequest>
    {
        public CalcularRotaValidator()
        {
            RuleFor(x => x.LatitudeOrigem).LatitudeValida("de origem");
            RuleFor(x => x.LongitudeOrigem).LongitudeValida("de origem");
            RuleFor(x => x.LatitudeDestino).LatitudeValida("de destino");
            RuleFor(x => x.LongitudeDestino).LongitudeValida("de destino");

            // Barrado aqui, e não no handler, porque é a chamada que não vale a pena fazer:
            // a Google cobraria por um trajeto de distância zero.
            RuleFor(x => x.LatitudeDestino)
                .Must((requisicao, _) => requisicao.LatitudeOrigem != requisicao.LatitudeDestino
                                      || requisicao.LongitudeOrigem != requisicao.LongitudeDestino)
                .WithMessage("Origem e destino não podem ser o mesmo ponto.");
        }
    }
}
