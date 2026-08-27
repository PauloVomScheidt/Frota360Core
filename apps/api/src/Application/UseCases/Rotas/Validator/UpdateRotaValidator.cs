using FluentValidation;
using Frota360.Application.DTOs.Rota.Request;

namespace Frota360.Application.UseCases.Rotas.Validator
{
    public class UpdateRotaValidator : AbstractValidator<UpdateRotaRequest>
    {
        public UpdateRotaValidator()
        {
            RuleFor(x => x.Origem)
                .NotEmpty().WithMessage("Origem é obrigatória.")
                .MaximumLength(100).WithMessage("Origem deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Destino)
                .NotEmpty().WithMessage("Destino é obrigatório.")
                .MaximumLength(150).WithMessage("Destino deve ter no máximo 150 caracteres.")
                .NotEqual(x => x.Origem).WithMessage("Destino não pode ser igual à origem.");

            RuleFor(x => x.CodigoMotorista)
                .GreaterThan(0).WithMessage("Motorista é obrigatório.");

            RuleFor(x => x.CodigoVeiculo)
                .GreaterThan(0).WithMessage("Veículo é obrigatório.");

            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("Data de início é obrigatória.");
        }
    }
}
