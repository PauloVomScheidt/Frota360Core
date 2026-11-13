import { act, screen } from '@testing-library/react'
import { Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import type { Role } from '../api/types'
import { pode } from '../auth/permissions'
import { renderizar } from '../test/renderizar'
import { RotaAtual } from '../test/RotaAtual'
import { RequireAuth, RequirePode } from './RequireAuth'

function arvore(rota: string, papel?: Role) {
  return renderizar(
    <Routes>
      <Route path="/login" element={<RotaAtual />} />
      <Route element={<RequireAuth />}>
        <Route path="/perfil" element={<h1>Meu perfil</h1>} />
        <Route element={<RequirePode permitido={pode.verDashboard} />}>
          <Route path="/dashboard" element={<h1>Visão geral</h1>} />
        </Route>
        <Route element={<RequirePode permitido={pode.verMinhasRotas} />}>
          <Route path="/minhas-rotas" element={<h1>Minhas rotas</h1>} />
        </Route>
        <Route element={<RequirePode permitido={pode.verAuditoria} />}>
          <Route path="/auditoria" element={<h1>Auditoria</h1>} />
        </Route>
      </Route>
    </Routes>,
    { rota, papel },
  )
}

describe('RequireAuth', () => {
  it.each(['/perfil', '/dashboard', '/auditoria'])('deslogado em %s vai para /login, guardando a origem', (rota) => {
    arvore(rota)
    expect(screen.getByRole('status', { name: 'Rota atual' })).toHaveTextContent(`/login (origem: ${rota})`)
  })

  it('logado, a tela protegida abre', () => {
    arvore('/perfil', 'Motorista')
    expect(screen.getByRole('heading', { name: 'Meu perfil' })).toBeInTheDocument()
  })

  it('sair em outra aba derruba esta para o login', () => {
    arvore('/perfil', 'Operador')
    expect(screen.getByRole('heading', { name: 'Meu perfil' })).toBeInTheDocument()

    act(() => {
      localStorage.clear()
      window.dispatchEvent(new StorageEvent('storage'))
    })

    expect(screen.getByRole('status', { name: 'Rota atual' })).toHaveTextContent('/login')
  })
})

describe('RequirePode', () => {
  it.each<[Role, string, string]>([
    ['Motorista', '/dashboard', 'Minhas rotas'],
    ['Motorista', '/auditoria', 'Minhas rotas'],
    ['Operador', '/minhas-rotas', 'Visão geral'],
    ['Operador', '/auditoria', 'Visão geral'],
    ['Supervisor', '/auditoria', 'Visão geral'],
  ])('%s sem permissão para %s cai na própria home (%s)', (papel, rota, home) => {
    arvore(rota, papel)
    expect(screen.getByRole('heading', { name: home })).toBeInTheDocument()
  })

  it.each<[Role, string, string]>([
    ['Admin', '/auditoria', 'Auditoria'],
    ['Supervisor', '/dashboard', 'Visão geral'],
    ['Motorista', '/minhas-rotas', 'Minhas rotas'],
  ])('%s com permissão para %s vê a tela', (papel, rota, titulo) => {
    arvore(rota, papel)
    expect(screen.getByRole('heading', { name: titulo })).toBeInTheDocument()
  })
})
