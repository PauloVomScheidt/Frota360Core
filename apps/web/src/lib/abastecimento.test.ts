import { describe, expect, it } from 'vitest'
import { valorTotalAbastecimento } from './abastecimento'

/** O que a API grava: decimal, duas casas, meio centavo para longe do zero. */
function valorDaApi(litrosMil: number, precoMil: number) {
  return Number((BigInt(litrosMil) * BigInt(precoMil) + 5000n) / 10000n) / 100
}

describe('valorTotalAbastecimento — espelho do valor que a API grava', () => {
  it.each([
    [48.5, 6.19, 300.22],
    [20.5, 5.39, 110.5],
    [1, 4.015, 4.02],
    [100, 6.5, 650],
    [0, 6.5, 0],
  ])('%s L × R$ %s = R$ %s', (litros, valorLitro, esperado) => {
    expect(valorTotalAbastecimento(litros, valorLitro)).toBe(esperado)
  })

  it('bate com a API em toda combinação de litros (1 casa) e preço (2 casas) de 20 a 150 L', () => {
    const divergencias: string[] = []
    for (let decimos = 200; decimos <= 1500; decimos += 5) {
      for (let centavos = 400; centavos <= 799; centavos++) {
        const litros = decimos / 10
        const preco = centavos / 100
        if (valorTotalAbastecimento(litros, preco) !== valorDaApi(decimos * 100, centavos * 10)) {
          divergencias.push(`${litros} × ${preco}`)
        }
      }
    }
    expect(divergencias).toEqual([])
  })

  it('bate com a API com três casas nos dois campos', () => {
    const divergencias: string[] = []
    for (let litrosMil = 1000; litrosMil <= 60_000; litrosMil += 37) {
      for (let precoMil = 4000; precoMil <= 7999; precoMil += 13) {
        if (valorTotalAbastecimento(litrosMil / 1000, precoMil / 1000) !== valorDaApi(litrosMil, precoMil)) {
          divergencias.push(`${litrosMil / 1000} × ${precoMil / 1000}`)
        }
      }
    }
    expect(divergencias).toEqual([])
  })
})
