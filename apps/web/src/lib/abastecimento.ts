/**
 * Espelho de `Math.Round(litros × valorLitro, 2, MidpointRounding.AwayFromZero)` da API
 * (`AbastecimentoMappings.cs`), que calcula em `decimal`. Em `double` o meio centavo escorrega:
 * 20,5 × 5,39 dá 110,49499… e arredondaria para 110,49, quando a API grava 110,50. Por isso a
 * conta é feita em inteiros — os dois campos têm no máximo três casas (`step` 0,001).
 */
export function valorTotalAbastecimento(litros: number, valorLitro: number): number {
  const produtoEmMilionesimos = Math.round(litros * 1000) * Math.round(valorLitro * 1000)
  const centavos = Math.floor((Math.abs(produtoEmMilionesimos) + 5000) / 10000)
  return (Math.sign(produtoEmMilionesimos) * centavos) / 100
}
