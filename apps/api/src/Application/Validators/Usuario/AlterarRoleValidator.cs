using FluentValidation;
using Frota360.Application.DTOs.Usuario.Request;
using Frota360.Domain.Common;

namespace Frota360.Application.Validators.Usuario
{
    public class AlterarRoleValidator : AbstractValidator<AlterarRoleRequest>
    {
        private static readonly string[] RolesValidas = [Roles.Admin, Roles.Supervisor, Roles.Operador, Roles.Motorista];

        public AlterarRoleValidator()
        {
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role é obrigatória.")
                .Must(r => RolesValidas.Contains(r))
                .WithMessage($"Role inválida. Valores aceitos: {string.Join(", ", RolesValidas)}.");
        }
    }
}
