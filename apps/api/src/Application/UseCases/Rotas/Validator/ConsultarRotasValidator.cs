using FluentValidation;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Rota.Request;

namespace Frota360.Application.UseCases.Rotas.Validator
{
    public class ConsultarRotasValidator : AbstractValidator<ConsultarRotasRequest>
    {
        public ConsultarRotasValidator() => this.AplicarRegrasDePaginacao();
    }
}
