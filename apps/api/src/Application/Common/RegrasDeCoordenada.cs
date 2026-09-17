using FluentValidation;

namespace Frota360.Application.Common
{
    /// <summary>
    /// Faixas de latitude e longitude, num lugar só. Três validators de rota as aplicam
    /// (create, update e o cálculo de trajeto) — repetir os limites em cada um convidaria a
    /// divergirem na próxima mudança.
    /// </summary>
    public static class RegrasDeCoordenada
    {
        public const decimal LatitudeMinima = -90m;
        public const decimal LatitudeMaxima = 90m;
        public const decimal LongitudeMinima = -180m;
        public const decimal LongitudeMaxima = 180m;

        public static IRuleBuilderOptions<T, decimal> LatitudeValida<T>(this IRuleBuilder<T, decimal> regra, string campo)
            => regra.InclusiveBetween(LatitudeMinima, LatitudeMaxima)
                    .WithMessage($"Latitude {campo} deve estar entre {LatitudeMinima} e {LatitudeMaxima}.");

        public static IRuleBuilderOptions<T, decimal> LongitudeValida<T>(this IRuleBuilder<T, decimal> regra, string campo)
            => regra.InclusiveBetween(LongitudeMinima, LongitudeMaxima)
                    .WithMessage($"Longitude {campo} deve estar entre {LongitudeMinima} e {LongitudeMaxima}.");

        public static IRuleBuilderOptions<T, decimal?> LatitudeValida<T>(this IRuleBuilder<T, decimal?> regra, string campo)
            => regra.InclusiveBetween(LatitudeMinima, LatitudeMaxima)
                    .WithMessage($"Latitude {campo} deve estar entre {LatitudeMinima} e {LatitudeMaxima}.");

        public static IRuleBuilderOptions<T, decimal?> LongitudeValida<T>(this IRuleBuilder<T, decimal?> regra, string campo)
            => regra.InclusiveBetween(LongitudeMinima, LongitudeMaxima)
                    .WithMessage($"Longitude {campo} deve estar entre {LongitudeMinima} e {LongitudeMaxima}.");
    }
}
