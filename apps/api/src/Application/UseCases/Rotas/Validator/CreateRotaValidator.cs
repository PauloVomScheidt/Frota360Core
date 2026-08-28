using FluentValidation;
using Frota360.Application.Common;
using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.Interfaces;

namespace Frota360.Application.UseCases.Rotas.Validator
{
    public class CreateRotaValidator : AbstractValidator<CreateRotaRequest>
    {
        public CreateRotaValidator(ICurrentUserService currentUser)
        {
            RuleFor(x => x.Origem)
                .NotEmpty().WithMessage("Origem é obrigatória.")
                .MaximumLength(100).WithMessage("Origem deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Destino)
                .NotEmpty().WithMessage("Destino é obrigatório.")
                .MaximumLength(150).WithMessage("Destino deve ter no máximo 150 caracteres.")
                .NotEqual(x => x.Origem).WithMessage("Destino não pode ser igual à origem.");

            // O motorista não escolhe o motorista da rota: o handler grava o da claim e
            // ignora o corpo, então exigir o campo dele seria pedir um dado que não usamos.
            RuleFor(x => x.CodigoMotorista)
                .GreaterThan(0).WithMessage("Motorista é obrigatório.")
                .Unless(_ => currentUser.EhMotorista());

            RuleFor(x => x.CodigoVeiculo)
                .GreaterThan(0).WithMessage("Veículo é obrigatório.");

            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("Data de início é obrigatória.");

            RuleFor(x => x.KmInicial)
                .GreaterThanOrEqualTo(0).WithMessage("Quilometragem inicial não pode ser negativa.")
                .LessThanOrEqualTo(2_000_000).WithMessage("Quilometragem inicial parece inválida.");
        }
    }
}
