namespace Frota360.Domain.Common
{
    /// <summary>
    /// Par latitude/longitude em graus decimais. <c>decimal</c>, e não <c>double</c>, pela mesma
    /// razão da coluna: coordenada é um valor exato que trafega e é comparado, não uma medida
    /// sobre a qual se faz aritmética de ponto flutuante.
    /// </summary>
    public sealed record Coordenada(decimal Latitude, decimal Longitude);
}
