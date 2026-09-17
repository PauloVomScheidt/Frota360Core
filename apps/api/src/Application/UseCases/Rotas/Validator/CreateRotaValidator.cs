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
            RuleFor(x => x.EnderecoPartida)
                .NotEmpty().WithMessage("Endereço de partida é obrigatório.")
                .MaximumLength(250).WithMessage("Endereço de partida deve ter no máximo 250 caracteres.");

            RuleFor(x => x.EnderecoChegada)
                .NotEmpty().WithMessage("Endereço de chegada é obrigatório.")
                .MaximumLength(250).WithMessage("Endereço de chegada deve ter no máximo 250 caracteres.")
                .NotEqual(x => x.EnderecoPartida).WithMessage("Endereço de chegada não pode ser igual ao de partida.");

            RuleFor(x => x.LatitudePartida).LatitudeValida("de partida").When(x => x.LatitudePartida is not null);
            RuleFor(x => x.LongitudePartida).LongitudeValida("de partida").When(x => x.LongitudePartida is not null);
            RuleFor(x => x.LatitudeChegada).LatitudeValida("de chegada").When(x => x.LatitudeChegada is not null);
            RuleFor(x => x.LongitudeChegada).LongitudeValida("de chegada").When(x => x.LongitudeChegada is not null);

            // O motorista não escolhe o motorista da rota: o handler grava o da claim e
            // ignora o corpo, então exigir o campo dele seria pedir um dado que não usamos.

            this.AplicarRegrasDeTrajeto();

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
