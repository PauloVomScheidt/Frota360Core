using FluentValidation;
using Frota360.Application.DTOs.Veiculo.Request;
using System.Text.RegularExpressions;

namespace Frota360.Application.UseCases.Veiculos.Validator
{
    public class UpdateVeiculoValidator : AbstractValidator<UpdateVeiculoRequest>
    {
        public UpdateVeiculoValidator()
        {
            RuleFor(x => x.NomeVeiculo)
                .NotEmpty().WithMessage("Nome do veículo é obrigatório.")
                .MaximumLength(100).WithMessage("Nome do veículo deve ter no máximo 100 caracteres.");

            RuleFor(x => x.MarcaVeiculo)
                .NotEmpty().WithMessage("Marca do veículo é obrigatória.")
                .MaximumLength(100).WithMessage("Marca do veículo deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Placa)
                .NotEmpty().WithMessage("Placa é obrigatória.")
                .MaximumLength(10).WithMessage("Placa deve ter no máximo 10 caracteres.")
                // RN09 — Mercosul (ABC1D23) ou antigo (ABC1234). A caixa é indiferente aqui:
                // o handler grava em maiúsculas, então recusar "abc1d23" seria 422 por caixa,
                // não por formato.
                .Matches(@"^[A-Z]{3}\d{4}$|^[A-Z]{3}\d[A-Z]\d{2}$", RegexOptions.IgnoreCase)
                .WithMessage("Placa inválida. Use o formato ABC1234 ou ABC1D23.");

            RuleFor(x => x.Quilometragem)
                .GreaterThanOrEqualTo(0).WithMessage("Quilometragem não pode ser negativa.");

            RuleFor(x => x.UltimoMotorista)
                .MaximumLength(100).WithMessage("Nome do motorista deve ter no máximo 100 caracteres.")
                .When(x => x.UltimoMotorista is not null);

            RuleFor(x => x.DataUltimaViagem)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Data da última viagem não pode ser futura.")
                .When(x => x.DataUltimaViagem is not null);
        }
    }
}
