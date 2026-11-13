import { screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import type { Role } from './api/types'
import { capturar, responderJson } from './test/capturar'
import { fabricarPerfil, fabricarResumo, paginado } from './test/fabricas'
import { renderizar } from './test/renderizar'
import App from './App'

/** O `App` traz o próprio `BrowserRouter`: a URL inicial vem do `history` do jsdom. */
function abrir(url: string, papel?: Role) {
  window.history.pushState({}, '', url)
  return renderizar(<App />, { roteador: false, papel })
}

afterEach(() => window.history.pushState({}, '', '/'))

describe('App — rotas públicas', () => {
  it('/ abre a landing page', () => {
    abrir('/')
    expect(screen.getByRole('heading', { level: 1, name: 'A revisão avisa antes de virar oficina.' })).toBeInTheDocument()
  })

  it.each([
    ['/login', 'Entrar'],
    ['/esqueci-senha', 'Esqueci minha senha'],
    ['/redefinir-senha', 'Link inválido'],
    ['/convite', 'Convite inválido'],
  ])('%s abre sem sessão', (url, titulo) => {
    abrir(url)
    expect(screen.getByRole('heading', { name: titulo })).toBeInTheDocument()
  })

  it('URL desconhecida volta para a landing', () => {
    abrir('/rota-que-nao-existe')
    expect(window.location.pathname).toBe('/')
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('A revisão avisa antes de virar oficina.')
  })
})

describe('App — rotas protegidas', () => {
  it.each(['/dashboard', '/perfil', '/minhas-rotas', '/auditoria'])('%s sem sessão vai para o login', (url) => {
    abrir(url)
    expect(window.location.pathname).toBe('/login')
    expect(screen.getByRole('heading', { name: 'Entrar' })).toBeInTheDocument()
  })

  it('/perfil logado abre para qualquer papel — o Motorista inclusive', async () => {
    capturar('get', '/usuario/perfil', responderJson(fabricarPerfil()))
    abrir('/perfil', 'Motorista')
    expect(await screen.findByRole('heading', { name: 'Meu perfil' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/perfil')
  })

  it('/veiculos logado abre a tela de veículos', async () => {
    abrir('/veiculos', 'Operador')
    expect(await screen.findByRole('heading', { name: 'Veículos' })).toBeInTheDocument()
  })

  it('o Motorista que tenta abrir /custos cai em /minhas-rotas', async () => {
    capturar('get', '/rota/minhas', responderJson(paginado([])))
    capturar('get', '/rota/resumo', responderJson({ quantidade: 0, kmTotal: 0 }))
    capturar('get', '/abastecimento/resumo', responderJson(fabricarResumo()))
    abrir('/custos', 'Motorista')

    expect(await screen.findByRole('heading', { name: 'Minhas rotas' })).toBeInTheDocument()
    expect(window.location.pathname).toBe('/minhas-rotas')
  })

  it('/login continua acessível logado — dá para trocar de conta', () => {
    abrir('/login', 'Admin')
    expect(screen.getByRole('heading', { name: 'Entrar' })).toBeInTheDocument()
  })
})
