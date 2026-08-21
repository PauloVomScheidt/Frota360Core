import type { ReactNode } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { logout } from '../api/auth'
import { pode } from '../auth/permissions'
import { notificarMudancaDeSessao, useSession } from '../auth/useSession'
import { BellIcon, ChevronDownIcon, LogoutIcon } from './icons'

function initials(nome: string): string {
  const partes = nome.trim().split(/\s+/)
  const primeira = partes[0]?.[0] ?? ''
  const ultima = partes.length > 1 ? (partes[partes.length - 1][0] ?? '') : ''
  return (primeira + ultima).toUpperCase() || '??'
}

function navLinkStyle({ isActive }: { isActive: boolean }) {
  return isActive ? { color: 'var(--color-accent)' } : undefined
}

export function AppLayout({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const user = useSession()
  const admin = pode.gerenciarUsuarios(user?.role)

  async function handleLogout() {
    await logout()
    notificarMudancaDeSessao()
    queryClient.clear()
    navigate('/login', { replace: true })
  }

  return (
    <div>
      <nav className="nav">
        <div className="mr-10 flex items-center gap-2.5">
          <div className="h-[22px] w-[22px] flex-none" style={{ background: 'var(--color-accent)' }} />
          <span className="nav-brand">FROTA 360</span>
        </div>

        <NavLink to="/" end style={navLinkStyle}>Visão geral</NavLink>
        {admin && (
          <>
            <NavLink to="/usuarios" style={navLinkStyle}>Usuários</NavLink>
            <NavLink to="/convites" style={navLinkStyle}>Convites</NavLink>
          </>
        )}

        <div className="ml-auto flex items-center gap-4">
          <button type="button" className="btn btn-icon" aria-label="Notificações">
            <BellIcon />
          </button>
          <div className="flex items-center gap-2">
            <div
              className="flex h-[30px] w-[30px] items-center justify-center rounded-full text-xs font-extrabold"
              style={{
                background: 'var(--color-neutral-300)',
                color: 'var(--color-neutral-800)',
                fontFamily: 'var(--font-heading)',
              }}
            >
              {initials(user?.nome ?? '')}
            </div>
            <div className="leading-tight">
              <div className="text-[13px]">{user?.nome ?? 'Usuário'}</div>
              <div
                className="text-[11px] uppercase"
                style={{ letterSpacing: '0.06em', color: 'color-mix(in srgb, var(--color-text) 55%, transparent)' }}
              >
                {user?.role ?? '—'}
              </div>
            </div>
            <ChevronDownIcon size={14} />
          </div>
          <button type="button" className="btn btn-icon" aria-label="Sair" onClick={handleLogout}>
            <LogoutIcon />
          </button>
        </div>
      </nav>

      <div className="mx-auto max-w-[1280px] px-10 py-8">{children}</div>
    </div>
  )
}

/** Cabeçalho padrão das páginas internas. */
export function PageHeader({
  titulo,
  subtitulo,
  acoes,
}: {
  titulo: string
  subtitulo?: string
  acoes?: ReactNode
}) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-3">
      <div>
        <h2 style={{ margin: '0 0 4px' }}>{titulo}</h2>
        {subtitulo && (
          <p
            className="m-0 text-[13px]"
            style={{ color: 'color-mix(in srgb, var(--color-text) 55%, transparent)' }}
          >
            {subtitulo}
          </p>
        )}
      </div>
      {acoes}
    </div>
  )
}

/** Lista de mensagens de erro no padrão do design (usada em formulários e telas). */
export function ErrorList({ mensagens }: { mensagens: string[] }) {
  if (mensagens.length === 0) return null
  return (
    <ul className="m-0 list-none p-0 text-[13px]" style={{ color: '#a03123' }}>
      {mensagens.map((msg) => (
        <li key={msg}>{msg}</li>
      ))}
    </ul>
  )
}
