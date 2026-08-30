import { useState, type CSSProperties, type ReactNode } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { logout } from '../api/auth'
import { pode } from '../auth/permissions'
import { notificarMudancaDeSessao, useSession } from '../auth/useSession'
import { iniciais } from '../lib/format'
import { LogoMark, Wordmark } from './Logo'
import {
  BellIcon,
  ChevronDownIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  FuelIcon,
  GridIcon,
  HistoricoIcon,
  LogoutIcon,
  ClipboardIcon,
  MailIcon,
  RouteIcon,
  TruckIcon,
  UsersIcon,
  WrenchIcon,
} from './icons'

const SIDEBAR_KEY = 'frota360.sidebarExpanded'

function lerPreferencia(chave: string, padrao: boolean): boolean {
  try {
    const raw = localStorage.getItem(chave)
    return raw === null ? padrao : raw === 'true'
  } catch {
    return padrao
  }
}

function gravarPreferencia(chave: string, valor: boolean) {
  try {
    localStorage.setItem(chave, String(valor))
  } catch {
    // Preferência é conveniência: se o storage falhar, segue sem persistir.
  }
}

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

function navItemStyle(expanded: boolean) {
  return ({ isActive }: { isActive: boolean }): CSSProperties => ({
    display: 'flex',
    alignItems: 'center',
    gap: 10,
    textDecoration: 'none',
    cursor: 'pointer',
    fontSize: 13,
    padding: expanded ? '9px 16px 9px 30px' : '11px 0',
    justifyContent: expanded ? 'flex-start' : 'center',
    color: isActive ? 'var(--color-accent-700)' : 'var(--color-text)',
    background: isActive ? 'var(--color-accent-100)' : 'transparent',
  })
}

interface ItemNav {
  to: string
  rotulo: string
  icone: ReactNode
  end?: boolean
}

function SidebarItem({ item, expanded }: { item: ItemNav; expanded: boolean }) {
  return (
    <NavLink
      to={item.to}
      end={item.end}
      style={navItemStyle(expanded)}
      title={expanded ? undefined : item.rotulo}
    >
      {item.icone}
      {expanded && <span className="whitespace-nowrap">{item.rotulo}</span>}
    </NavLink>
  )
}

function SidebarCategoria({
  titulo,
  itens,
  expanded,
  aberta,
  onToggle,
}: {
  titulo: string
  itens: ItemNav[]
  expanded: boolean
  aberta: boolean
  onToggle: () => void
}) {
  // Com a sidebar recolhida não há espaço para os títulos: os itens ficam sempre visíveis.
  const mostrarItens = !expanded || aberta

  return (
    <div className="mb-1.5">
      {expanded && (
        <button
          type="button"
          className="flex w-full cursor-pointer items-center justify-between gap-2 border-0 bg-transparent px-4 py-2 text-[11px] uppercase"
          style={{ fontFamily: 'var(--font-heading)', letterSpacing: '0.08em', color: mutedText }}
          onClick={onToggle}
          aria-expanded={aberta}
        >
          {titulo}
          <span
            className="flex"
            style={{ transform: aberta ? 'rotate(0deg)' : 'rotate(-90deg)', transition: 'transform 0.15s ease' }}
          >
            <ChevronDownIcon size={14} />
          </span>
        </button>
      )}
      {mostrarItens && itens.map((item) => <SidebarItem key={item.to} item={item} expanded={expanded} />)}
    </div>
  )
}

export function AppLayout({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const user = useSession()
  const admin = pode.gerenciarUsuarios(user?.role)
  const gestor = pode.editarTiposManutencao(user?.role)
  const motorista = pode.verMinhasRotas(user?.role)

  const [expanded, setExpanded] = useState(() => lerPreferencia(SIDEBAR_KEY, true))
  const [catDashboard, setCatDashboard] = useState(true)
  const [catVisualizacao, setCatVisualizacao] = useState(true)
  const [catControle, setCatControle] = useState(true)

  function toggleSidebar() {
    setExpanded((v) => {
      gravarPreferencia(SIDEBAR_KEY, !v)
      return !v
    })
  }

  async function handleLogout() {
    await logout()
    notificarMudancaDeSessao()
    queryClient.clear()
    navigate('/login', { replace: true })
  }

  // Para o motorista a sidebar separa o que ele **faz** do que ele só **consulta**:
  // rotas e abastecimentos são escrita, veículos e manutenções são leitura (saber o
  // estado do caminhão faz parte do trabalho, mas ele não mexe em nenhum dos dois).
  const itensDashboard: ItemNav[] = motorista
    ? [
        { to: '/minhas-rotas', rotulo: 'Minhas rotas', icone: <RouteIcon size={17} /> },
        { to: '/abastecimentos', rotulo: 'Abastecimentos', icone: <FuelIcon size={17} /> },
      ]
    : [
        { to: '/dashboard', rotulo: 'Visão geral', icone: <GridIcon size={17} /> },
        { to: '/motoristas', rotulo: 'Motoristas', icone: <UsersIcon size={17} /> },
        { to: '/veiculos', rotulo: 'Veículos', icone: <TruckIcon size={17} /> },
        { to: '/rotas', rotulo: 'Rotas', icone: <RouteIcon size={17} /> },
        { to: '/manutencoes', rotulo: 'Manutenções', icone: <WrenchIcon size={17} /> },
        { to: '/abastecimentos', rotulo: 'Abastecimentos', icone: <FuelIcon size={17} /> },
        // O catálogo de tipos só aparece para quem pode mantê-lo (Admin/Supervisor).
        ...(gestor
          ? [{ to: '/tipos-manutencao', rotulo: 'Tipos de manutenção', icone: <ClipboardIcon size={17} /> }]
          : []),
      ]

  /** Só do motorista: as duas telas que ele alcança em leitura. */
  const itensVisualizacao: ItemNav[] = [
    { to: '/veiculos', rotulo: 'Veículos', icone: <TruckIcon size={17} /> },
    { to: '/manutencoes', rotulo: 'Manutenções', icone: <WrenchIcon size={17} /> },
  ]

  // A categoria inteira já é admin-only, então nenhum item aqui precisa de guarda própria.
  const itensControle: ItemNav[] = [
    { to: '/usuarios', rotulo: 'Usuários', icone: <UsersIcon size={17} /> },
    { to: '/convites', rotulo: 'Convites', icone: <MailIcon size={17} /> },
    { to: '/auditoria', rotulo: 'Auditoria', icone: <HistoricoIcon size={17} /> },
  ]

  return (
    <div className="flex min-h-screen">
      <aside
        className="flex flex-none flex-col overflow-hidden"
        style={{
          width: expanded ? 236 : 64,
          background: 'var(--color-bg)',
          borderRight: '2px solid var(--color-divider)',
        }}
      >
        <div
          className="flex items-center gap-2.5"
          style={{
            borderBottom: '2px solid var(--color-divider)',
            minHeight: 59,
            padding: expanded ? '18px 16px' : '18px 0',
            justifyContent: expanded ? undefined : 'center',
          }}
        >
          {expanded && (
            <>
              <LogoMark size={30} />
              <Wordmark size={18} />
            </>
          )}
          <button
            type="button"
            className="flex h-6 w-6 flex-none cursor-pointer items-center justify-center border-0 bg-transparent p-0"
            style={{ color: mutedText, lineHeight: 0, marginLeft: expanded ? 'auto' : 0 }}
            onClick={toggleSidebar}
            aria-label="Expandir ou recolher menu"
          >
            {expanded ? <ChevronLeftIcon size={16} /> : <ChevronRightIcon size={16} />}
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto py-3">
          <SidebarCategoria
            titulo={motorista ? 'Operação' : 'Dashboard'}
            itens={itensDashboard}
            expanded={expanded}
            aberta={catDashboard}
            onToggle={() => setCatDashboard((v) => !v)}
          />
          {motorista && (
            <SidebarCategoria
              titulo="Visualização"
              itens={itensVisualizacao}
              expanded={expanded}
              aberta={catVisualizacao}
              onToggle={() => setCatVisualizacao((v) => !v)}
            />
          )}
          {admin && (
            <SidebarCategoria
              titulo="Controle"
              itens={itensControle}
              expanded={expanded}
              aberta={catControle}
              onToggle={() => setCatControle((v) => !v)}
            />
          )}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header
          className="flex items-center justify-end gap-4 px-10 py-3"
          style={{ borderBottom: '2px solid var(--color-divider)' }}
        >
          <button type="button" className="btn btn-icon" aria-label="Notificações">
            <BellIcon />
          </button>
          {/* O bloco do avatar é o caminho para `/perfil` — a tela onde a pessoa corrige os
              próprios dados. Sem gate de papel: vale para todas as roles. */}
          <NavLink
            to="/perfil"
            className="flex items-center gap-2"
            style={{ color: 'inherit', textDecoration: 'none' }}
            title="Meu perfil"
          >
            <div
              className="flex h-[30px] w-[30px] items-center justify-center rounded-full text-xs font-extrabold"
              style={{
                background: 'var(--color-neutral-300)',
                color: 'var(--color-neutral-800)',
                fontFamily: 'var(--font-heading)',
              }}
            >
              {iniciais(user?.nome ?? '')}
            </div>
            <div className="leading-tight">
              <div className="text-[13px]">{user?.nome ?? 'Usuário'}</div>
              <div className="text-[11px] uppercase" style={{ letterSpacing: '0.06em', color: mutedText }}>
                {user?.role ?? '—'}
              </div>
            </div>
            <ChevronDownIcon size={14} />
          </NavLink>
          <button type="button" className="btn btn-icon" aria-label="Sair" onClick={handleLogout}>
            <LogoutIcon />
          </button>
        </header>

        <main className="mx-auto w-full max-w-[1280px] px-10 py-8">{children}</main>
      </div>
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
          <p className="m-0 text-[13px]" style={{ color: mutedText }}>
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
    <ul className="m-0 list-none p-0 text-[13px]" style={{ color: 'var(--color-danger)' }}>
      {mensagens.map((msg) => (
        <li key={msg}>{msg}</li>
      ))}
    </ul>
  )
}
