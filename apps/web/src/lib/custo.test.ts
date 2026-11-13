import { describe, expect, it } from 'vitest'
import type { CustoPorMesResponse } from '../api/types'
import {
  escalaDeTresMarcas,
  formatConsumo,
  formatCustoPorKm,
  nomeDoMes,
  percentual,
  preencherMeses,
  ROTULO_ORIGEM,
  rotuloDoMes,
  SERIES_CUSTO,
} from './custo'

const texto = (valor: string) => valor.replace(/\s/g, ' ')

function mes(ano: number, numero: number, abastecimento = 0, manutencao = 0, despesa = 0): CustoPorMesResponse {
  return {
    ano,
    mes: numero,
    totalAbastecimento: abastecimento,
    totalManutencao: manutencao,
    totalDespesa: despesa,
    total: abastecimento + manutencao + despesa,
  }
}

describe('ROTULO_ORIGEM', () => {
  it('nomeia as três origens de custo, com acento', () => {
    expect(ROTULO_ORIGEM).toEqual({ Abastecimento: 'Abastecimento', Manutencao: 'Manutenção', Despesa: 'Despesa' })
  })
})

describe('preencherMeses', () => {
  it('completa com zero os meses sem lançamento, na ordem do calendário', () => {
    const resultado = preencherMeses([mes(2026, 8, 100)], { de: '2026-06-01', ate: '2026-09-15' })
    expect(resultado.map((m) => `${m.mes}/${m.ano}`)).toEqual(['6/2026', '7/2026', '8/2026', '9/2026'])
    expect(resultado.map((m) => m.total)).toEqual([0, 0, 100, 0])
  })

  it('mantém os valores que a API devolveu', () => {
    const agosto = mes(2026, 8, 100, 50, 25)
    expect(preencherMeses([agosto], { de: '2026-08-01', ate: '2026-08-31' })).toEqual([agosto])
  })

  it('atravessa a virada de ano', () => {
    const resultado = preencherMeses([mes(2027, 1, 10)], { de: '2026-11-01', ate: '2027-02-10' })
    expect(resultado.map((m) => `${m.mes}/${m.ano}`)).toEqual(['11/2026', '12/2026', '1/2027', '2/2027'])
    expect(resultado[2].total).toBe(10)
  })

  it('um cliente sem lançamento nenhum ainda vê os seis meses', () => {
    expect(preencherMeses([], { de: '2026-06-01', ate: '2026-11-12' })).toHaveLength(6)
  })
})

describe('escalaDeTresMarcas', () => {
  it.each([0, -50])('sem custo a escala é só o zero (%s)', (maximo) => {
    expect(escalaDeTresMarcas(maximo)).toEqual({ teto: 1, marcas: [0] })
  })

  it.each([
    [4800, 2500, 5000],
    [9000, 5000, 10_000],
    [48_200, 25_000, 50_000],
    [1, 1, 2],
  ])('exemplo: máximo %s → passo %s e teto %s', (maximo, passo, teto) => {
    expect(escalaDeTresMarcas(maximo)).toEqual({ teto, marcas: [0, passo, teto] })
  })

  it.each([0.4, 1, 7, 99, 100, 101, 999, 1000, 1001, 2500, 4999, 5000, 5001, 10_000, 12_345, 99_999, 1_000_000])(
    'com máximo %s: três marcas, passo "limpo" e teto acima do maior valor',
    (maximo) => {
      const { teto, marcas } = escalaDeTresMarcas(maximo)
      const [, passo] = marcas
      const fator = passo / 10 ** Math.floor(Math.log10(passo))

      expect(marcas).toEqual([0, passo, passo * 2])
      expect(teto).toBe(passo * 2)
      expect(teto).toBeGreaterThan(maximo)
      expect([1, 2, 2.5, 5].some((f) => Math.abs(f - fator) < 1e-9)).toBe(true)
    },
  )
})

describe('rotuloDoMes', () => {
  it.each([
    [2026, 8, 'ago/26'],
    [2027, 1, 'jan/27'],
    [2026, 12, 'dez/26'],
  ])('%s-%s → %s', (ano, numero, esperado) => {
    expect(rotuloDoMes(ano, numero)).toBe(esperado)
  })

  it('mês fora do intervalo vira interrogação', () => {
    expect(rotuloDoMes(2026, 13)).toBe('?/26')
  })
})

describe('formatCustoPorKm', () => {
  it('mostra até quatro casas, porque o valor costuma ficar abaixo de um real', () => {
    expect(texto(formatCustoPorKm(0.4567))).toBe('R$ 0,4567/km')
    expect(texto(formatCustoPorKm(0.5))).toBe('R$ 0,50/km')
  })

  it.each([null, undefined])('sem rota encerrada não existe métrica (%s)', (valor) => {
    expect(formatCustoPorKm(valor)).toBe('—')
  })
})

describe('formatConsumo de custos', () => {
  it('mostra uma casa', () => {
    expect(formatConsumo(8.46)).toBe('8,5 km/l')
  })

  it.each([null, undefined])('com menos de dois abastecimentos não existe consumo (%s)', (valor) => {
    expect(formatConsumo(valor)).toBe('—')
  })
})

describe('SERIES_CUSTO', () => {
  it('empilha combustível, manutenção e despesas, nessa ordem', () => {
    expect(SERIES_CUSTO.map((s) => s.rotulo)).toEqual(['Combustível', 'Manutenção', 'Despesas'])
  })

  it('cada série lê o próprio total do mês', () => {
    const m = mes(2026, 8, 100, 50, 25)
    expect(SERIES_CUSTO.map((s) => s.valor(m))).toEqual([100, 50, 25])
  })

  it('usa os degraus 500/400/300 do acento, não as cores de situação', () => {
    expect(SERIES_CUSTO.map((s) => s.cor)).toEqual([
      'var(--color-accent-500)',
      'var(--color-accent-400)',
      'var(--color-accent-300)',
    ])
  })
})

describe('nomeDoMes', () => {
  it('nome por extenso com inicial maiúscula', () => {
    expect(nomeDoMes(mes(2026, 8))).toBe('Agosto de 2026')
  })
})

describe('percentual', () => {
  it.each([
    [25, 100, '25%'],
    [1, 3, '33%'],
    [2, 3, '67%'],
    [100, 100, '100%'],
  ])('%s de %s → %s', (parte, total, esperado) => {
    expect(percentual(parte, total)).toBe(esperado)
  })
})
