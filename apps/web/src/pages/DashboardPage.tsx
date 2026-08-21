import { useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { motoristasApi } from '../api/motoristas'
import { veiculosApi } from '../api/veiculos'
import { rotasApi } from '../api/rotas'
import { logout } from '../api/auth'
import { tokenStorage } from '../api/tokenStorage'
import {
  BellIcon,
  ChevronDownIcon,
  LogoutIcon,
  RouteIcon,
  SearchIcon,
  TruckIcon,
  UsersIcon,
  WrenchIcon,
} from '../components/icons'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('pt-BR')
}

function formatKm(km: number): string {
  return `${km.toLocaleString('pt-BR')} km`
}

function initials(nome: string): string {
  const partes = nome.trim().split(/\s+/)
  const primeira = partes[0]?.[0] ?? ''
  const ultima = partes.length > 1 ? (partes[partes.length - 1][0] ?? '') : ''
  return (primeira + ultima).toUpperCase() || '??'
}

interface Kpi {
  label: string
  value: string
  detail: string
  icon: ReactNode
}

export function DashboardPage() {
  const navigate = useNavigate()
  const [busca, setBusca] = useState('')
  const user = tokenStorage.getUser()

  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })
  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })
  const rotasQuery = useQuery({ queryKey: ['rotas'], queryFn: rotasApi.getAll })

  const veiculos = veiculosQuery.data ?? []
  const motoristas = motoristasQuery.data ?? []
  const rotas = rotasQuery.data ?? []

  const kpis: Kpi[] = [
    {
      label: 'Veículos',
      value: veiculosQuery.isSuccess ? String(veiculos.length) : '—',
      detail: 'cadastrados na frota',
      icon: <TruckIcon />,
    },
    {
      label: 'Motoristas',
      value: motoristasQuery.isSuccess ? String(motoristas.length) : '—',
      detail: 'cadastrados',
      icon: <UsersIcon />,
    },
    {
      label: 'Rotas ativas',
      value: rotasQuery.isSuccess ? String(rotas.filter((r) => r.ativo).length) : '—',
      detail: rotasQuery.isSuccess ? `de ${rotas.length} rotas no total` : '',
      icon: <RouteIcon />,
    },
    {
      label: 'Km da frota',
      value: veiculosQuery.isSuccess
        ? veiculos.reduce((soma, v) => soma + v.quilometragem, 0).toLocaleString('pt-BR')
        : '—',
      detail: 'quilometragem acumulada',
      icon: <WrenchIcon />,
    },
  ]

  const veiculosFiltrados = useMemo(() => {
    const lista = veiculosQuery.data ?? []
    const termo = busca.trim().toLowerCase()
    if (!termo) return lista
    return lista.filter((v) =>
      [v.placa, v.nomeVeiculo, v.marcaVeiculo, v.ultimoMotorista ?? '']
        .join(' ')
        .toLowerCase()
        .includes(termo),
    )
  }, [veiculosQuery.data, busca])

  const atualizadoEm = veiculosQuery.dataUpdatedAt
    ? `Atualizado às ${new Date(veiculosQuery.dataUpdatedAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`
    : veiculosQuery.isError
      ? 'Não foi possível carregar os dados'
      : 'Carregando dados…'

  async function handleLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div>
      <nav className="nav">
        <div className="mr-10 flex items-center gap-2.5">
          <div className="h-[22px] w-[22px] flex-none" style={{ background: 'var(--color-accent)' }} />
          <span className="nav-brand">FROTA 360</span>
        </div>
        <a href="#" aria-current="page">Visão geral</a>
        <a href="#veiculos">Veículos</a>
        <a href="#">Motoristas</a>
        <a href="#">Rotas</a>
        <div className="ml-auto flex items-center gap-4">
          <button type="button" className="btn btn-icon" aria-label="Notificações">
            <BellIcon />
          </button>
          <div className="flex cursor-pointer items-center gap-2">
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
            <span className="text-[13px]">{user?.nome ?? 'Usuário'}</span>
            <ChevronDownIcon size={14} />
          </div>
          <button type="button" className="btn btn-icon" aria-label="Sair" onClick={handleLogout}>
            <LogoutIcon />
          </button>
        </div>
      </nav>

      <div className="mx-auto max-w-[1280px] px-10 py-8">
        <div className="mb-6 flex flex-wrap items-end justify-between gap-3">
          <div>
            <h2 style={{ margin: '0 0 4px' }}>Visão geral da frota</h2>
            <p className="m-0 text-[13px]" style={{ color: mutedText }}>
              {atualizadoEm}
            </p>
          </div>
        </div>

        <div
          className="mb-8 grid grid-cols-2 lg:grid-cols-4"
          style={{ border: '1px solid var(--color-divider)' }}
        >
          {kpis.map((kpi) => (
            <div
              key={kpi.label}
              className="flex flex-col gap-2 p-5"
              style={{ borderRight: '1px solid var(--color-divider)', background: 'var(--color-surface)' }}
            >
              <div className="flex items-center justify-between">
                <span
                  className="text-[11px] uppercase"
                  style={{ letterSpacing: '0.08em', color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}
                >
                  {kpi.label}
                </span>
                {kpi.icon}
              </div>
              <span
                className="text-[30px] leading-none font-extrabold"
                style={{ fontFamily: 'var(--font-heading)' }}
              >
                {kpi.value}
              </span>
              <span className="text-xs" style={{ color: 'var(--color-accent-700)' }}>
                {kpi.detail}
              </span>
            </div>
          ))}
        </div>

        <div id="veiculos" className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h3 style={{ margin: 0 }}>Veículos</h3>
          <div className="relative w-[260px]">
            <span
              className="absolute top-0 bottom-0 left-2.5 flex items-center"
              style={{ color: 'color-mix(in srgb, var(--color-text) 50%, transparent)' }}
            >
              <SearchIcon size={14} />
            </span>
            <input
              className="input"
              type="text"
              placeholder="Buscar veículo ou motorista"
              style={{ borderRadius: 0, paddingLeft: 32 }}
              value={busca}
              onChange={(e) => setBusca(e.target.value)}
            />
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Veículo</th>
                <th>Marca</th>
                <th>Último motorista</th>
                <th>Quilometragem</th>
                <th>Última viagem</th>
                <th>Incluído em</th>
              </tr>
            </thead>
            <tbody>
              {veiculosQuery.isPending && (
                <tr>
                  <td colSpan={6} style={{ color: mutedText }}>Carregando veículos…</td>
                </tr>
              )}
              {veiculosQuery.isError && (
                <tr>
                  <td colSpan={6} style={{ color: '#a03123' }}>
                    Não foi possível carregar os veículos. Tente novamente em instantes.
                  </td>
                </tr>
              )}
              {veiculosQuery.isSuccess && veiculosFiltrados.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ color: mutedText }}>
                    {busca ? 'Nenhum veículo encontrado para a busca.' : 'Nenhum veículo cadastrado ainda.'}
                  </td>
                </tr>
              )}
              {veiculosFiltrados.map((v) => (
                <tr key={v.id}>
                  <td className="font-semibold">
                    {v.placa}
                    <div className="text-xs font-normal" style={{ color: mutedText }}>
                      {v.nomeVeiculo}
                    </div>
                  </td>
                  <td>{v.marcaVeiculo}</td>
                  <td>{v.ultimoMotorista || '—'}</td>
                  <td>{formatKm(v.quilometragem)}</td>
                  <td>
                    {v.dataUltimaViagem ? (
                      <span className="tag tag-accent">{formatDate(v.dataUltimaViagem)}</span>
                    ) : (
                      <span className="tag tag-neutral">Sem viagens</span>
                    )}
                  </td>
                  <td>{formatDate(v.dataInclusao)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
