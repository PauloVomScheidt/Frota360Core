import { afterEach, describe, expect, it, vi } from 'vitest'
import { gravarPreferencia, lerPreferencia, lerPreferenciaBooleana } from './preferencias'

const numero = (bruto: string) => (Number.isNaN(Number(bruto)) ? null : Number(bruto))

afterEach(() => vi.restoreAllMocks())

describe('lerPreferencia', () => {
  it('sem nada gravado usa o padrão', () => {
    expect(lerPreferencia('chave', 15, numero)).toBe(15)
  })

  it('lê o valor gravado', () => {
    localStorage.setItem('chave', '20')
    expect(lerPreferencia('chave', 15, numero)).toBe(20)
  })

  it('valor que o decodificador recusa cai no padrão', () => {
    localStorage.setItem('chave', 'abc')
    expect(lerPreferencia('chave', 15, numero)).toBe(15)
  })

  it('storage bloqueado (aba anônima) cai no padrão em vez de quebrar a tela', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('SecurityError')
    })
    expect(lerPreferencia('chave', 15, numero)).toBe(15)
  })
})

describe('gravarPreferencia', () => {
  it('grava o valor como texto', () => {
    gravarPreferencia('sidebar', true)
    gravarPreferencia('tamanho', 20)
    expect(localStorage.getItem('sidebar')).toBe('true')
    expect(localStorage.getItem('tamanho')).toBe('20')
  })

  it('storage cheio ou bloqueado não quebra a tela', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('QuotaExceededError')
    })
    expect(() => gravarPreferencia('tamanho', 20)).not.toThrow()
  })
})

describe('lerPreferenciaBooleana', () => {
  it('sem nada gravado usa o padrão', () => {
    expect(lerPreferenciaBooleana('sidebar', true)).toBe(true)
    expect(lerPreferenciaBooleana('sidebar', false)).toBe(false)
  })

  it('lê o que foi gravado, sobrepondo o padrão', () => {
    gravarPreferencia('sidebar', false)
    expect(lerPreferenciaBooleana('sidebar', true)).toBe(false)
    gravarPreferencia('sidebar', true)
    expect(lerPreferenciaBooleana('sidebar', false)).toBe(true)
  })
})
