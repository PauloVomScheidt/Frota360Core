import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  erroDoIntervalo,
  filtroDePeriodo,
  intervaloDoFiltro,
  intervaloDoPeriodo,
  mesAteHoje,
  mesmoPeriodoDoMesPassado,
  paraIso,
  PERIODOS,
  ultimosMeses,
  type Periodo,
} from './periodo'

function congelarEm(data: Date) {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(data)
}

afterEach(() => vi.useRealTimers())

describe('PERIODOS', () => {
  it('oferece "Personalizado…" por último, depois dos períodos prontos', () => {
    expect(PERIODOS.at(-1)).toEqual({ valor: 'personalizado', rotulo: 'Personalizado…' })
  })

  it('tem um rótulo para cada período', () => {
    expect(PERIODOS.every((p) => p.rotulo.length > 0)).toBe(true)
    expect(new Set(PERIODOS.map((p) => p.valor)).size).toBe(PERIODOS.length)
  })

  it('cria o filtro sem datas livres', () => {
    expect(filtroDePeriodo('7dias')).toEqual({ periodo: '7dias', de: '', ate: '' })
  })
})

describe('intervaloDoPeriodo em 12/11/2026', () => {
  beforeEach(() => congelarEm(new Date(2026, 10, 12, 10, 0)))

  it.each<[Periodo, { de?: string; ate?: string }]>([
    ['todos', {}],
    ['hoje', { de: '2026-11-12', ate: '2026-11-12' }],
    ['7dias', { de: '2026-11-06', ate: '2026-11-12' }],
    ['30dias', { de: '2026-10-14', ate: '2026-11-12' }],
    ['esteMes', { de: '2026-11-01', ate: '2026-11-30' }],
    ['mesPassado', { de: '2026-10-01', ate: '2026-10-31' }],
    ['3meses', { de: '2026-09-01', ate: '2026-11-30' }],
    ['6meses', { de: '2026-06-01', ate: '2026-11-30' }],
    ['esteAno', { de: '2026-01-01', ate: '2026-12-31' }],
    ['anoPassado', { de: '2025-01-01', ate: '2025-12-31' }],
    ['personalizado', {}],
  ])('%s → %o', (periodo, esperado) => {
    expect(intervaloDoPeriodo(periodo)).toEqual(esperado)
  })

  it('"últimos 7 dias" contam hoje e os 6 anteriores', () => {
    const { de, ate } = intervaloDoPeriodo('7dias')
    const dias = (new Date(`${ate}T00:00:00`).getTime() - new Date(`${de}T00:00:00`).getTime()) / 86_400_000 + 1
    expect(dias).toBe(7)
  })

  it('"este mês" vai até o último dia do mês, para alcançar o que ainda vai vencer', () => {
    expect(intervaloDoPeriodo('esteMes').ate).toBe('2026-11-30')
  })
})

describe('intervaloDoPeriodo na virada de ano (03/01/2027)', () => {
  beforeEach(() => congelarEm(new Date(2027, 0, 3, 8, 0)))

  it.each<[Periodo, { de?: string; ate?: string }]>([
    ['7dias', { de: '2026-12-28', ate: '2027-01-03' }],
    ['30dias', { de: '2026-12-05', ate: '2027-01-03' }],
    ['mesPassado', { de: '2026-12-01', ate: '2026-12-31' }],
    ['3meses', { de: '2026-11-01', ate: '2027-01-31' }],
    ['6meses', { de: '2026-08-01', ate: '2027-01-31' }],
    ['esteAno', { de: '2027-01-01', ate: '2027-12-31' }],
    ['anoPassado', { de: '2026-01-01', ate: '2026-12-31' }],
  ])('%s → %o', (periodo, esperado) => {
    expect(intervaloDoPeriodo(periodo)).toEqual(esperado)
  })
})

describe('intervaloDoPeriodo na virada de mês', () => {
  it('em 01/03/2026 o mês passado é fevereiro de 28 dias', () => {
    congelarEm(new Date(2026, 2, 1, 9, 0))
    expect(intervaloDoPeriodo('mesPassado')).toEqual({ de: '2026-02-01', ate: '2026-02-28' })
    expect(intervaloDoPeriodo('7dias')).toEqual({ de: '2026-02-23', ate: '2026-03-01' })
  })

  it('em 01/03/2028 (bissexto) o mês passado termina em 29/02', () => {
    congelarEm(new Date(2028, 2, 1, 9, 0))
    expect(intervaloDoPeriodo('mesPassado')).toEqual({ de: '2028-02-01', ate: '2028-02-29' })
  })

  it('em 31/12 "este mês" termina no próprio dia 31', () => {
    congelarEm(new Date(2026, 11, 31, 12, 0))
    expect(intervaloDoPeriodo('esteMes')).toEqual({ de: '2026-12-01', ate: '2026-12-31' })
  })
})

describe('hora local', () => {
  it('"hoje" às 23h30 continua sendo hoje, mesmo que em UTC já seja amanhã', () => {
    congelarEm(new Date(2026, 10, 12, 23, 30))
    expect(intervaloDoPeriodo('hoje')).toEqual({ de: '2026-11-12', ate: '2026-11-12' })
  })

  it('"hoje" à 00h10 não volta para ontem', () => {
    congelarEm(new Date(2026, 10, 12, 0, 10))
    expect(intervaloDoPeriodo('hoje')).toEqual({ de: '2026-11-12', ate: '2026-11-12' })
  })
})

describe('intervaloDoFiltro', () => {
  it('nos períodos prontos ignora de/ate e usa o calendário', () => {
    congelarEm(new Date(2026, 10, 12, 10, 0))
    expect(intervaloDoFiltro({ periodo: 'hoje', de: '2020-01-01', ate: '2020-01-31' })).toEqual({
      de: '2026-11-12',
      ate: '2026-11-12',
    })
  })

  it('no personalizado usa as datas digitadas', () => {
    expect(intervaloDoFiltro({ periodo: 'personalizado', de: '2026-01-10', ate: '2026-02-20' })).toEqual({
      de: '2026-01-10',
      ate: '2026-02-20',
    })
  })

  it('aceita só "a partir de" ou só "até"', () => {
    expect(intervaloDoFiltro({ periodo: 'personalizado', de: '2026-01-10', ate: '' })).toEqual({
      de: '2026-01-10',
      ate: undefined,
    })
    expect(intervaloDoFiltro({ periodo: 'personalizado', de: '', ate: '2026-02-20' })).toEqual({
      de: undefined,
      ate: '2026-02-20',
    })
  })

  it('personalizado sem datas não filtra nada', () => {
    expect(intervaloDoFiltro(filtroDePeriodo('personalizado'))).toEqual({ de: undefined, ate: undefined })
  })
})

describe('erroDoIntervalo', () => {
  it('recusa data final anterior à inicial', () => {
    expect(erroDoIntervalo('2026-11-12', '2026-11-11')).toBe('A data final não pode ser anterior à inicial.')
  })

  it.each([
    ['2026-11-12', '2026-11-12'],
    ['2026-12-31', '2027-01-01'],
    ['2026-11-12', ''],
    ['', '2026-11-12'],
    ['', ''],
  ])('aceita %s → %s', (de, ate) => {
    expect(erroDoIntervalo(de, ate)).toBeNull()
  })
})

describe('paraIso', () => {
  it('formata a data local como aaaa-mm-dd com zero à esquerda', () => {
    expect(paraIso(new Date(2026, 0, 5, 23, 59))).toBe('2026-01-05')
  })
})

describe('intervalos fixos do dashboard', () => {
  it('mesAteHoje vai do dia 1 até hoje', () => {
    expect(mesAteHoje(new Date(2026, 10, 12))).toEqual({ de: '2026-11-01', ate: '2026-11-12' })
  })

  it('mesAteHoje usa o relógio quando não recebe a data', () => {
    congelarEm(new Date(2026, 0, 1, 7, 0))
    expect(mesAteHoje()).toEqual({ de: '2026-01-01', ate: '2026-01-01' })
  })

  it.each([
    [new Date(2026, 10, 12), { de: '2026-10-01', ate: '2026-10-12' }],
    [new Date(2026, 2, 31), { de: '2026-02-01', ate: '2026-02-28' }],
    [new Date(2028, 2, 31), { de: '2028-02-01', ate: '2028-02-29' }],
    [new Date(2026, 0, 15), { de: '2025-12-01', ate: '2025-12-15' }],
    [new Date(2026, 4, 31), { de: '2026-04-01', ate: '2026-04-30' }],
  ])('mesmoPeriodoDoMesPassado compara trecho igual, sem invadir o mês corrente (%s)', (hoje, esperado) => {
    expect(mesmoPeriodoDoMesPassado(hoje)).toEqual(esperado)
  })

  it('mesmoPeriodoDoMesPassado usa o relógio quando não recebe a data', () => {
    congelarEm(new Date(2027, 0, 31, 7, 0))
    expect(mesmoPeriodoDoMesPassado()).toEqual({ de: '2026-12-01', ate: '2026-12-31' })
  })

  it('ultimosMeses inclui o mês corrente até hoje', () => {
    expect(ultimosMeses(6, new Date(2026, 10, 12))).toEqual({ de: '2026-06-01', ate: '2026-11-12' })
    expect(ultimosMeses(1, new Date(2026, 10, 12))).toEqual({ de: '2026-11-01', ate: '2026-11-12' })
  })

  it('ultimosMeses atravessa a virada de ano', () => {
    expect(ultimosMeses(6, new Date(2026, 1, 10))).toEqual({ de: '2025-09-01', ate: '2026-02-10' })
  })

  it('ultimosMeses usa o relógio quando não recebe a data', () => {
    congelarEm(new Date(2026, 10, 12, 7, 0))
    expect(ultimosMeses(3)).toEqual({ de: '2026-09-01', ate: '2026-11-12' })
  })
})
