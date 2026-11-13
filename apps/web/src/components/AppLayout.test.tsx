import { screen, within } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { tokenStorage } from '../api/tokenStorage'
import type { Role } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarSessao } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { RotaAtual } from '../test/RotaAtual'
import { AppLayout, PageHeader } from './AppLayout'

/**
 * As telas internas e quem as alcança — a tabela do §2 de docs/contexto-web.md, escrita a
 * partir da documentação. `/perfil` fica de fora: não é item de menu, é o bloco do avatar.
 */
const TELAS: Record<string, Role[]> = {
  '/dashboard': ['Admin', 'Supervisor', 'Operador'],
  '/motoristas': ['Admin', 'Supervisor', 'Operador'],
  '/veiculos': ['Admin', 'Supervisor', 'Operador', 'Motorista'],
  '/rotas': ['Admin', 'Supervisor', 'Operador'],
  '/manutencoes': ['Admin', 'Supervisor', 'Operador', 'Motorista'],
  '/abastecimentos': ['Admin', 'Supervisor', 'Operador', 'Motorista'],
  '/despesas': ['Admin', 'Supervisor', 'Operador'],
  '/custos': ['Admin', 'Supervisor', 'Operador'],
  '/minhas-rotas': ['Motorista'],
  '/tipos-manutencao': ['Admin', 'Supervisor'],
  '/tipos-despesa': ['Admin', 'Supervisor'],
  '/tipos-combustivel': ['Admin', 'Supervisor'],
  '/postos': ['Admin', 'Supervisor'],
  '/usuarios': ['Admin'],
  '/convites': ['Admin'],
  '/auditoria': ['Admin'],
}

const telasDo = (papel: Role) =>
  Object.entries(TELAS)
    .filter(([, papeis]) => papeis.includes(papel))
    .map(([rota]) => rota)
    .sort()

function layout(papel: Role = 'Admin', nomeEmpresa: string | null = 'Transportes Horizonte Ltda') {
  tokenStorage.setSession(fabricarSessao({ nome: 'Ana Souza', role: papel, nomeEmpresa: nomeEmpresa ?? undefined }))
  return renderizar(
    <Routes>
      <Route
        path="/painel"
        element={
          <AppLayout>
            <PageHeader titulo="Conteúdo da tela" subtitulo="Subtítulo" acoes={<button type="button">Ação</button>} />
          </AppLayout>
        }
      />
      <Route path="*" element={<RotaAtual />} />
    </Routes>,
    { rota: '/painel' },
  )
}

const menu = () => within(screen.getByRole('navigation'))
const rotasDoMenu = () =>
  menu()
    .getAllByRole('link')
    .map((link) => link.getAttribute('href'))
    .sort()

describe('AppLayout — menu por papel', () => {
  it.each<Role>(['Admin', 'Supervisor', 'Operador', 'Motorista'])(
    '%s vê no menu exatamente as telas que pode abrir',
    (papel) => {
      layout(papel)
      expect(rotasDoMenu()).toEqual(telasDo(papel))
    },
  )

  it.each<[Role, string[]]>([
    ['Admin', ['Dashboard', 'Parametrização', 'Controle']],
    ['Supervisor', ['Dashboard', 'Parametrização']],
    ['Operador', ['Dashboard']],
    ['Motorista', ['Operação', 'Visualização']],
  ])('%s vê as categorias %o', (papel, categorias) => {
    layout(papel)
    expect(menu().getAllByRole('button').map((b) => b.textContent)).toEqual(categorias)
  })

  it('recolher uma categoria esconde os itens dela', async () => {
    const { user } = layout('Admin')
    const controle = menu().getByRole('button', { name: 'Controle' })
    expect(controle).toHaveAttribute('aria-expanded', 'true')

    await user.click(controle)

    expect(controle).toHaveAttribute('aria-expanded', 'false')
    expect(menu().queryByRole('link', { name: 'Auditoria' })).not.toBeInTheDocument()
    expect(menu().getByRole('link', { name: 'Postos' })).toBeInTheDocument()
  })

  it('recolher a sidebar some com os rótulos, mantém os itens e lembra a preferência', async () => {
    const { user } = layout('Operador')
    await user.click(screen.getByRole('button', { name: 'Expandir ou recolher menu' }))

    expect(localStorage.getItem('frota360.sidebarExpanded')).toBe('false')
    expect(menu().queryByText('Custos')).not.toBeInTheDocument()
    expect(menu().getByTitle('Custos')).toHaveAttribute('href', '/custos')
    expect(rotasDoMenu()).toEqual(telasDo('Operador'))
  })

  it('a sidebar abre recolhida para quem a deixou assim', () => {
    localStorage.setItem('frota360.sidebarExpanded', 'false')
    layout('Operador')
    expect(menu().queryByRole('button', { name: 'Dashboard' })).not.toBeInTheDocument()
  })
})

describe('AppLayout — cabeçalho', () => {
  it('mostra o nome da empresa, com o nome inteiro no title', () => {
    layout('Supervisor', 'Transportes Horizonte Ltda')
    const empresa = screen.getByText('Transportes Horizonte Ltda')
    expect(empresa).toBeInTheDocument()
    expect(empresa.parentElement).toHaveAttribute('title', 'Transportes Horizonte Ltda')
  })

  it('sessão antiga sem nome da empresa não quebra: o bloco só não aparece', () => {
    layout('Supervisor', null)
    expect(screen.queryByText('Transportes Horizonte Ltda')).not.toBeInTheDocument()
    expect(screen.getByText('Ana Souza')).toBeInTheDocument()
  })

  it('o bloco do usuário leva ao perfil, com iniciais, nome e papel', () => {
    layout('Motorista')
    const perfil = screen.getByRole('link', { name: /Ana Souza/ })
    expect(perfil).toHaveAttribute('href', '/perfil')
    expect(perfil).toHaveTextContent('AS')
    expect(perfil).toHaveTextContent('Motorista')
  })

  it('o PageHeader mostra título, subtítulo e a ação da tela', () => {
    layout()
    expect(screen.getByRole('heading', { name: 'Conteúdo da tela' })).toBeInTheDocument()
    expect(screen.getByText('Subtítulo')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Ação' })).toBeInTheDocument()
  })
})

describe('AppLayout — sair', () => {
  it('chama POST /auth/logout, limpa a sessão e o cache e vai para o login', async () => {
    const sair = capturar('post', '/auth/logout', responderJson(null))
    const { user, queryClient } = layout('Admin')
    queryClient.setQueryData(['veiculos'], ['dado da empresa'])

    await user.click(screen.getByRole('button', { name: 'Sair' }))

    expect(await screen.findByRole('status', { name: 'Rota atual' })).toHaveTextContent('/login')
    expect(sair).toHaveLength(1)
    expect(tokenStorage.getUser()).toBeNull()
    expect(queryClient.getQueryData(['veiculos'])).toBeUndefined()
  })

  it.each([
    ['500', responderErro(500, ['Erro interno.'])],
    ['rede fora', () => Response.error()],
  ])('sair funciona mesmo com a API respondendo %s', async (_, resposta) => {
    capturar('post', '/auth/logout', resposta)
    const { user, queryClient } = layout('Admin')
    queryClient.setQueryData(['veiculos'], ['dado da empresa'])

    await user.click(screen.getByRole('button', { name: 'Sair' }))

    expect(await screen.findByRole('status', { name: 'Rota atual' })).toHaveTextContent('/login')
    expect(tokenStorage.getUser()).toBeNull()
    expect(queryClient.getQueryData(['veiculos'])).toBeUndefined()
  })
})
