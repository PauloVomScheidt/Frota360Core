import { afterEach, describe, expect, it, vi } from 'vitest'
import { fabricarRota } from '../test/fabricas'
import { DIAS_ALERTA_ROTA_ABERTA, diasEmAberto, statusDaRota } from './rota'

describe('statusDaRota', () => {
  it('rota ativa está em curso (azul)', () => {
    expect(statusDaRota(fabricarRota({ ativo: true, dataFim: null }))).toEqual({
      rotulo: 'Ativa',
      classe: 'tag tag-accent',
    })
  })

  it('rota com data de fim foi encerrada com sucesso (verde)', () => {
    expect(statusDaRota(fabricarRota({ ativo: false, dataFim: '2026-11-11T18:00:00' }))).toEqual({
      rotulo: 'Encerrada',
      classe: 'tag tag-success',
    })
  })

  it('rota inativa sem data de fim não tem estado (cinza)', () => {
    expect(statusDaRota(fabricarRota({ ativo: false, dataFim: null }))).toEqual({
      rotulo: 'Inativa',
      classe: 'tag tag-neutral',
    })
  })

  it('"ativa" prevalece mesmo que venha uma data de fim', () => {
    expect(statusDaRota(fabricarRota({ ativo: true, dataFim: '2026-11-11T18:00:00' })).rotulo).toBe('Ativa')
  })
})

describe('diasEmAberto', () => {
  afterEach(() => vi.useRealTimers())

  it('o alerta de rota esquecida começa em 2 dias', () => {
    expect(DIAS_ALERTA_ROTA_ABERTA).toBe(2)
  })

  it('conta zero no mesmo dia', () => {
    const rota = fabricarRota({ dataInicio: '2026-11-12T00:00:00' })
    expect(diasEmAberto(rota, new Date(2026, 10, 12, 23, 59))).toBe(0)
  })

  it('conta dias de calendário, não blocos de 24 horas', () => {
    const rota = fabricarRota({ dataInicio: '2026-11-11T23:00:00' })
    expect(diasEmAberto(rota, new Date(2026, 10, 12, 0, 30))).toBe(1)
  })

  it('atravessa a virada de mês e de ano', () => {
    expect(diasEmAberto(fabricarRota({ dataInicio: '2026-10-31T08:00:00' }), new Date(2026, 10, 2))).toBe(2)
    expect(diasEmAberto(fabricarRota({ dataInicio: '2026-12-30T08:00:00' }), new Date(2027, 0, 2))).toBe(3)
  })

  it('usa o relógio quando não recebe a data de hoje', () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 12, 9, 0))
    expect(diasEmAberto(fabricarRota({ dataInicio: '2026-11-10T00:00:00' }))).toBe(DIAS_ALERTA_ROTA_ABERTA)
  })
})
