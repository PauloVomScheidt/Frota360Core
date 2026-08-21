import { useMemo, useState, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { motoristasApi } from '../api/motoristas'
import { veiculosApi } from '../api/veiculos'
import { rotasApi } from '../api/rotas'
import { mensagensDeErro } from '../api/errors'
import { AppLayout, PageHeader } from '../components/AppLayout'
import { RouteIcon, SearchIcon, TruckIcon, UsersIcon, WrenchIcon } from '../components/icons'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('pt-BR')
}

function formatKm(km: number): string {
  return `${km.toLocaleString('pt-BR')} km`
}

interface Kpi {
  label: string
  value: string
  detail: string
  icon: ReactNode
}

export function DashboardPage() {
  const [busca, setBusca] = useState('')

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

  return (
    <AppLayout>
      <PageHeader titulo="Visão geral da frota" subtitulo={atualizadoEm} />

      <div className="mb-8 grid grid-cols-2 lg:grid-cols-4" style={{ border: '1px solid var(--color-divider)' }}>
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
            <span className="text-[30px] leading-none font-extrabold" style={{ fontFamily: 'var(--font-heading)' }}>
              {kpi.value}
            </span>
            <span className="text-xs" style={{ color: 'var(--color-accent-700)' }}>
              {kpi.detail}
            </span>
          </div>
        ))}
      </div>

      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
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
                <td colSpan={6} style={{ color: mutedText }}>
                  Carregando veículos…
                </td>
              </tr>
            )}
            {veiculosQuery.isError && (
              <tr>
                <td colSpan={6} style={{ color: '#a03123' }}>
                  {mensagensDeErro(veiculosQuery.error, 'Não foi possível carregar os veículos.')[0]}
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
    </AppLayout>
  )
}
