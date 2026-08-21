using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;

namespace Frota360.Application.Validators.Usuario
{
    public class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenValidator()
        {
            RuleFor(r => r.RefreshToken)
                .NotEmpty().WithMessage("O refresh token é obrigatório.");
        }
    }
}
