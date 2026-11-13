import { act, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderizar } from '../test/renderizar'
import { LandingPage } from './LandingPage'

/** IntersectionObserver falso: guarda o callback e o que foi observado. */
function instalarObservador() {
  const observados: Element[] = []
  const desobservados: Element[] = []
  let avisar: IntersectionObserverCallback = () => {}
  class ObservadorFalso {
    constructor(callback: IntersectionObserverCallback) {
      avisar = callback
    }
    observe(el: Element) {
      observados.push(el)
    }
    unobserve(el: Element) {
      desobservados.push(el)
    }
    disconnect() {}
  }
  vi.stubGlobal('IntersectionObserver', ObservadorFalso)
  return {
    observados,
    desobservados,
    entrarNaTela: (el: Element) =>
      act(() => avisar([{ isIntersecting: true, target: el } as IntersectionObserverEntry], {} as IntersectionObserver)),
  }
}

function preferirMenosMovimento(prefere: boolean) {
  vi.stubGlobal('matchMedia', (consulta: string) => ({ matches: prefere && consulta.includes('reduce') }))
}

afterEach(() => vi.unstubAllGlobals())

describe('LandingPage', () => {
  it('apresenta o produto e leva ao login, sem chamar a API', () => {
    renderizar(<LandingPage />)
    expect(screen.getByRole('heading', { level: 1, name: 'A revisão avisa antes de virar oficina.' })).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: /Entrar/ })[0]).toHaveAttribute('href', '/login')
  })

  it('a matriz de permissões diz "Sim"/"Não" em texto, não só com ícone', () => {
    renderizar(<LandingPage />)
    expect(screen.getAllByText('Sim').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Não').length).toBeGreaterThan(0)
  })

  it('as áreas roláveis são alcançáveis pelo teclado', () => {
    renderizar(<LandingPage />)
    const rolaveis = screen.getAllByRole('region')
    expect(rolaveis.length).toBeGreaterThan(0)
    for (const area of rolaveis) expect(area).toHaveAttribute('tabindex', '0')
  })

  it('o pedido de demonstração abre o e-mail já preenchido, sem afirmar que abriu', async () => {
    const location = { ...window.location, href: '' }
    vi.stubGlobal('location', location)
    const { user } = renderizar(<LandingPage />)

    await user.type(screen.getByRole('textbox', { name: 'Seu nome' }), 'Maria Lima')
    await user.type(screen.getByRole('textbox', { name: 'Empresa' }), 'Transportes Lima')
    await user.type(screen.getByRole('textbox', { name: 'E-mail de trabalho' }), 'maria@lima.com.br')
    const frota = screen.getByRole('combobox', { name: 'Tamanho da frota' })
    await user.selectOptions(frota, within(frota).getAllByRole('option')[1])
    await user.click(screen.getByRole('button', { name: 'Quero ver o painel' }))

    expect(location.href).toMatch(/^mailto:/)
    const corpo = decodeURIComponent(location.href.split('&body=')[1])
    expect(corpo).toContain('Nome: Maria Lima')
    expect(corpo).toContain('Empresa: Transportes Lima')
    expect(corpo).toContain('E-mail: maria@lima.com.br')
    expect(screen.getByText(/deve ter aberto com os dados preenchidos/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'chame no WhatsApp' })).toBeInTheDocument()
  })

  it('as faixas aparecem conforme entram na tela, e cada uma só é observada até aparecer', () => {
    preferirMenosMovimento(false)
    const io = instalarObservador()
    renderizar(<LandingPage />)

    expect(io.observados.length).toBeGreaterThan(0)
    io.entrarNaTela(io.observados[0])
    expect(io.desobservados).toEqual([io.observados[0]])
  })

  it('quem prefere menos movimento não tem nada escondido esperando animação', () => {
    preferirMenosMovimento(true)
    const io = instalarObservador()
    renderizar(<LandingPage />)
    expect(io.observados).toHaveLength(0)
  })
})
