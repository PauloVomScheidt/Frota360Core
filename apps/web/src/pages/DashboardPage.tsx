import { useMemo, useState, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { motoristasApi } from '../api/motoristas'
import { veiculosApi } from '../api/veiculos'
import { rotasApi } from '../api/rotas'
import { AppLayout, PageHeader } from '../components/AppLayout'
import { TableStates } from '../components/Table'
import { formatDate, formatKm } from '../lib/format'
import {
  ClipboardIcon,
  RouteIcon,
  SearchIcon,
  TruckIcon,
  UsersIcon,
  WrenchIcon,
} from '../components/icons'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

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

  /**
   * Km rodado no mês corrente: soma de `kmPercorrido` das rotas encerradas dentro do
   * mês (o recorte é por `dataFim`, o momento em que a quilometragem foi apurada).
   * `kmPercorrido` é persistido pela API — nunca recalcular a partir de kmInicial/kmFinal.
   */
  const kmDoMes = useMemo(() => {
    const agora = new Date()
    return (rotasQuery.data ?? []).reduce(
      (acumulado, rota) => {
        if (rota.kmPercorrido == null || !rota.dataFim) return acumulado
        const fim = new Date(rota.dataFim)
        if (Number.isNaN(fim.getTime())) return acumulado
        if (fim.getFullYear() !== agora.getFullYear() || fim.getMonth() !== agora.getMonth()) {
          return acumulado
        }
        return { km: acumulado.km + rota.kmPercorrido, rotas: acumulado.rotas + 1 }
      },
      { km: 0, rotas: 0 },
    )
  }, [rotasQuery.data])

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
    {
      label: 'Km rodado',
      value: rotasQuery.isSuccess ? kmDoMes.km.toLocaleString('pt-BR') : '—',
      detail: rotasQuery.isSuccess
        ? `no mês · ${kmDoMes.rotas} ${kmDoMes.rotas === 1 ? 'rota encerrada' : 'rotas encerradas'}`
        : '',
      icon: <ClipboardIcon />,
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

      <div className="mb-8 grid grid-cols-2 lg:grid-cols-5" style={{ border: '1px solid var(--color-divider)' }}>
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
            <TableStates
              colSpan={6}
              pending={veiculosQuery.isPending}
              error={veiculosQuery.error}
              empty={veiculosQuery.isSuccess && veiculosFiltrados.length === 0}
              textoCarregando="Carregando veículos…"
              textoErro="Não foi possível carregar os veículos."
              textoVazio={busca ? 'Nenhum veículo encontrado para a busca.' : 'Nenhum veículo cadastrado ainda.'}
            />
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
