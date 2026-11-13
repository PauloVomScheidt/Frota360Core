import { describe, expect, it } from 'vitest'
import { fabricarManutencao } from '../test/fabricas'
import { badgeDaManutencao, estaVencendo, FAIXA_AVISO, textoKmRestantes } from './manutencao'

describe('FAIXA_AVISO', () => {
  it('é 500 km, o mesmo corte de RegrasDeManutencao.FaixaAvisoKm na API', () => {
    expect(FAIXA_AVISO).toBe(500)
  })
})

describe('faixa de aviso de uma manutenção pendente', () => {
  it.each([
    [501, false, 'Pendente', 'tag tag-accent'],
    [500, true, 'Vencendo', 'tag tag-warning'],
    [499, true, 'Vencendo', 'tag tag-warning'],
    [0, true, 'Vencendo', 'tag tag-warning'],
  ])('com %s km restantes: vencendo=%s, etiqueta %s', (kmRestantes, vencendo, rotulo, classe) => {
    const m = fabricarManutencao({ status: 'Pendente', atrasada: false, kmRestantes })
    expect(estaVencendo(m)).toBe(vencendo)
    expect(badgeDaManutencao(m)).toEqual({ rotulo, classe })
  })

  it('sem km restantes informado não está vencendo', () => {
    const m = fabricarManutencao({ status: 'Pendente', atrasada: false, kmRestantes: null })
    expect(estaVencendo(m)).toBe(false)
    expect(badgeDaManutencao(m).rotulo).toBe('Pendente')
  })
})

describe('precedência de "atrasada"', () => {
  it('atrasada vence a faixa de aviso: não aparece como "vencendo"', () => {
    const m = fabricarManutencao({ status: 'Pendente', atrasada: true, kmRestantes: 300 })
    expect(estaVencendo(m)).toBe(false)
    expect(badgeDaManutencao(m)).toEqual({ rotulo: 'Atrasada', classe: 'tag tag-danger' })
  })

  it('atrasada pela data mesmo com km sobrando', () => {
    const m = fabricarManutencao({ status: 'Pendente', atrasada: true, kmRestantes: 8000 })
    expect(badgeDaManutencao(m).rotulo).toBe('Atrasada')
  })

  it('atrasada com o veículo já além do km previsto', () => {
    const m = fabricarManutencao({ status: 'Pendente', atrasada: true, kmRestantes: -120 })
    expect(badgeDaManutencao(m).rotulo).toBe('Atrasada')
  })

  it('atrasada prevalece sobre qualquer status', () => {
    const m = fabricarManutencao({ status: 'Realizada', atrasada: true })
    expect(badgeDaManutencao(m).rotulo).toBe('Atrasada')
  })
})

describe('manutenções que não estão pendentes', () => {
  it('realizada aparece como concluída (verde) e nunca como vencendo', () => {
    const m = fabricarManutencao({ status: 'Realizada', atrasada: false, kmRestantes: 100 })
    expect(estaVencendo(m)).toBe(false)
    expect(badgeDaManutencao(m)).toEqual({ rotulo: 'Concluída', classe: 'tag tag-success' })
  })

  it('cancelada não tem estado (cinza)', () => {
    const m = fabricarManutencao({ status: 'Cancelada', atrasada: false, kmRestantes: 100 })
    expect(estaVencendo(m)).toBe(false)
    expect(badgeDaManutencao(m)).toEqual({ rotulo: 'Cancelada', classe: 'tag tag-neutral' })
  })
})

describe('textoKmRestantes', () => {
  it.each([null, undefined])('sem km não há texto (%s)', (km) => {
    expect(textoKmRestantes(km)).toBeNull()
  })

  it('km positivo é o quanto falta', () => {
    expect(textoKmRestantes(1500)).toBe('faltam 1.500 km')
    expect(textoKmRestantes(0)).toBe('faltam 0 km')
  })

  it('km negativo é atraso, sem sinal de menos', () => {
    expect(textoKmRestantes(-1320)).toBe('1.320 km em atraso')
  })
})
