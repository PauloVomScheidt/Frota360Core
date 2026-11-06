using FluentValidation;
using Frota360.Application.DTOs.Convite.Request;

namespace Frota360.Application.Validators.Convite
{
    public class ConsultarConviteValidator : AbstractValidator<ConsultarConviteRequest>
    {
        public ConsultarConviteValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("O token do convite é obrigatório.");
        }
    }
}
