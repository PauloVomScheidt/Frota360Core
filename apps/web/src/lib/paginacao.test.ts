import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { TAMANHOS_PAGINA, usePaginacao, usePaginacaoServidor, useTamanhoPagina } from './paginacao'

const CHAVE = 'frota360.itensPorPagina'
const lista = (n: number) => Array.from({ length: n }, (_, i) => i + 1)

describe('useTamanhoPagina', () => {
  it('oferece 10, 15 e 20 itens por página', () => {
    expect(TAMANHOS_PAGINA).toEqual([10, 15, 20])
  })

  it('começa em 15', () => {
    const { result } = renderHook(() => useTamanhoPagina())
    expect(result.current.tamanhoPagina).toBe(15)
  })

  it('lembra a escolha gravada', () => {
    localStorage.setItem(CHAVE, '20')
    const { result } = renderHook(() => useTamanhoPagina())
    expect(result.current.tamanhoPagina).toBe(20)
  })

  it('ignora um tamanho gravado que não está entre as opções', () => {
    localStorage.setItem(CHAVE, '50')
    const { result } = renderHook(() => useTamanhoPagina())
    expect(result.current.tamanhoPagina).toBe(15)
  })

  it('é uma preferência do painel inteiro: outra tela abre com o tamanho escolhido', () => {
    const tela1 = renderHook(() => useTamanhoPagina())
    act(() => tela1.result.current.setTamanhoPagina(10))

    const tela2 = renderHook(() => useTamanhoPagina())
    expect(tela1.result.current.tamanhoPagina).toBe(10)
    expect(tela2.result.current.tamanhoPagina).toBe(10)
    expect(localStorage.getItem(CHAVE)).toBe('10')
  })
})

describe('usePaginacao (corte no cliente)', () => {
  it('fatia a lista na página corrente e conta o total da lista inteira', () => {
    const { result } = renderHook(() => usePaginacao(lista(40)))
    expect(result.current.itensDaPagina).toEqual(lista(15))
    expect(result.current).toMatchObject({ pagina: 1, totalPaginas: 3, total: 40, tamanhoPagina: 15 })
  })

  it('a última página traz só o que sobrou', () => {
    const { result } = renderHook(() => usePaginacao(lista(40)))
    act(() => result.current.onMudar(3))
    expect(result.current.itensDaPagina).toEqual([31, 32, 33, 34, 35, 36, 37, 38, 39, 40])
  })

  it('quando um filtro encolhe a lista, volta para uma página que existe — sem tela vazia', () => {
    const { result, rerender } = renderHook(({ itens }) => usePaginacao(itens), {
      initialProps: { itens: lista(40) },
    })
    act(() => result.current.onMudar(3))

    rerender({ itens: lista(8) })
    expect(result.current.pagina).toBe(1)
    expect(result.current.itensDaPagina).toEqual(lista(8))
  })

  it('lista vazia tem uma página, vazia', () => {
    const { result } = renderHook(() => usePaginacao([]))
    expect(result.current).toMatchObject({ itensDaPagina: [], pagina: 1, totalPaginas: 1, total: 0 })
  })

  it('trocar o tamanho refaz o corte e grava a preferência', () => {
    const { result } = renderHook(() => usePaginacao(lista(40)))
    act(() => result.current.onMudarTamanho(20))
    expect(result.current.totalPaginas).toBe(2)
    expect(result.current.itensDaPagina).toHaveLength(20)
    expect(localStorage.getItem(CHAVE)).toBe('20')
  })
})

describe('usePaginacaoServidor', () => {
  const resposta = { pagina: 2, totalPaginas: 7, total: 100, tamanhoPagina: 15 }

  it('começa na página 1 com o tamanho preferido', () => {
    localStorage.setItem(CHAVE, '20')
    const { result } = renderHook(() => usePaginacaoServidor())
    expect(result.current.pagina).toBe(1)
    expect(result.current.tamanhoPagina).toBe(20)
  })

  it('antes da primeira resposta o rodapé fica zerado (e some)', () => {
    const { result } = renderHook(() => usePaginacaoServidor())
    expect(result.current.props(undefined)).toMatchObject({ pagina: 1, totalPaginas: 1, total: 0, tamanhoPagina: 15 })
  })

  it('o rodapé mostra o que o servidor respondeu', () => {
    const { result } = renderHook(() => usePaginacaoServidor())
    expect(result.current.props(resposta)).toMatchObject(resposta)
  })

  it('mudar de página altera a página pedida ao servidor', () => {
    const { result } = renderHook(() => usePaginacaoServidor())
    act(() => result.current.props(resposta).onMudar(4))
    expect(result.current.pagina).toBe(4)
  })

  it('resetar volta para a página 1 — obrigatório a cada mudança de filtro', () => {
    const { result } = renderHook(() => usePaginacaoServidor())
    act(() => result.current.props(resposta).onMudar(4))
    act(() => result.current.resetar())
    expect(result.current.pagina).toBe(1)
  })

  it('trocar o tamanho grava a preferência e volta para a página 1', () => {
    const { result } = renderHook(() => usePaginacaoServidor())
    act(() => result.current.props(resposta).onMudar(4))
    act(() => result.current.props(resposta).onMudarTamanho(10))
    expect(result.current.pagina).toBe(1)
    expect(result.current.tamanhoPagina).toBe(10)
    expect(localStorage.getItem(CHAVE)).toBe('10')
  })
})
