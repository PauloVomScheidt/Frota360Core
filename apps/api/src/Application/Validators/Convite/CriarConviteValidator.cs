using FluentValidation;
using Frota360.Application.DTOs.Convite.Request;
using Frota360.Domain.Common;

namespace Frota360.Application.Validators.Convite
{
    public class CriarConviteValidator : AbstractValidator<CriarConviteRequest>
    {
        private static readonly string[] RolesValidas = [Roles.Admin, Roles.Supervisor, Roles.Operador, Roles.Motorista];

        public CriarConviteValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("E-mail inválido.")
                .MaximumLength(150).WithMessage("E-mail deve ter no máximo 150 caracteres.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role é obrigatória.")
                .Must(r => RolesValidas.Contains(r))
                .WithMessage($"Role inválida. Valores aceitos: {string.Join(", ", RolesValidas)}.");
        }
    }
}
