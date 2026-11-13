import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { custosApi } from '../api/custos'
import { manutencoesApi } from '../api/manutencoes'
import { rotasApi } from '../api/rotas'
import { veiculosApi } from '../api/veiculos'
import type {
  CustoPorVeiculoResponse,
  ManutencaoResponse,
  ResumoCustosResponse,
  ResumoManutencoes,
  RotaResponse,
} from '../api/types'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { PainelCustoMensal } from '../components/PainelCustoMensal'
import { TableStates } from '../components/Table'
import { DinheiroIcon, FuelIcon, RouteIcon, TruckIcon, WrenchIcon } from '../components/icons'
import {
  formatDate,
  formatDistancia,
  formatDuracao,
  formatKm,
  formatMoeda,
  formatMoedaCompacta,
} from '../lib/format'
import { formatConsumo, formatCustoPorKm, preencherMeses } from '../lib/custo'
import { FAIXA_AVISO, textoKmRestantes } from '../lib/manutencao'
import { mesAteHoje, mesmoPeriodoDoMesPassado, ultimosMeses, type Intervalo } from '../lib/periodo'
import { DIAS_ALERTA_ROTA_ABERTA, diasEmAberto } from '../lib/rota'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const MAXIMO_ROTAS_EM_ANDAMENTO = 5
const MAXIMO_RANKING = 5

interface Kpi {
  label: string
  value: ReactNode
  detail: string
  icon: ReactNode
  title?: string
  cor?: string
}

interface Pendencia {
  chave: string
  tom: 'danger' | 'warning'
  rotulo: string
  titulo: string
  contexto: string
  detalhe: string
  destino: string
  acao: string
}

function nomeDoMes(data: Date, comAno = false): string {
  return data.toLocaleDateString('pt-BR', comAno ? { month: 'long', year: 'numeric' } : { month: 'long' })
}

function variacaoDoCusto(atual: ResumoCustosResponse, anterior: ResumoCustosResponse, comparacao: Intervalo): string {
  const [ano, mes, dia] = comparacao.ate.split('-').map(Number)
  const periodo = `1 a ${dia} de ${nomeDoMes(new Date(ano, mes - 1, 1))}`

  if (anterior.total <= 0) return atual.total > 0 ? `nada lançado de ${periodo}` : 'nenhum lançamento no mês'

  const percentual = ((atual.total - anterior.total) / anterior.total) * 100
  const sinal = percentual >= 0 ? '+' : '−'
  return `${sinal}${Math.abs(percentual).toLocaleString('pt-BR', { maximumFractionDigits: 1 })}% vs. ${periodo}`
}

function plural(quantidade: number, singular: string, pluralizado: string): string {
  return `${quantidade} ${quantidade === 1 ? singular : pluralizado}`
}

function detalheDaManutencao(m: ManutencaoResponse): string {
  if (!m.atrasada) return textoKmRestantes(m.kmRestantes) ?? ''
  if (m.kmRestantes != null && m.kmRestantes < 0) return textoKmRestantes(m.kmRestantes) ?? ''
  if (m.kmRestantes === 0) return 'chegou ao km previsto'
  return m.dataPrevista ? `prazo venceu em ${formatDate(m.dataPrevista)}` : 'prazo vencido'
}

function montarPendencias(
  manutencoes: ResumoManutencoes | undefined,
  rotasAtivas: RotaResponse[],
  custoDoMes: ResumoCustosResponse | undefined,
): Pendencia[] {
  const pendencias: Pendencia[] = []

  for (const m of manutencoes?.urgentes ?? []) {
    pendencias.push({
      chave: `manutencao-${m.id}`,
      tom: m.atrasada ? 'danger' : 'warning',
      rotulo: m.atrasada ? 'Atrasada' : 'Vencendo',
      titulo: m.tipoManutencaoNome,
      contexto: `${m.veiculoPlaca} · ${m.veiculoNome}`,
      detalhe: detalheDaManutencao(m),
      destino: '/manutencoes',
      acao: 'Manutenções',
    })
  }

  if (manutencoes) {
    const foraDaLista = manutencoes.atrasadas + manutencoes.vencendo - manutencoes.urgentes.length
    if (foraDaLista > 0) {
      pendencias.push({
        chave: 'manutencao-restantes',
        tom: 'warning',
        rotulo: 'Manutenção',
        titulo: `Mais ${plural(foraDaLista, 'manutenção', 'manutenções')}`,
        contexto: 'atrasadas ou vencendo',
        detalhe: 'fora desta lista',
        destino: '/manutencoes',
        acao: 'Manutenções',
      })
    }
  }

  for (const rota of rotasAtivas) {
    const dias = diasEmAberto(rota)
    if (dias < DIAS_ALERTA_ROTA_ABERTA) continue
    pendencias.push({
      chave: `rota-${rota.id}`,
      tom: 'warning',
      rotulo: 'Rota aberta',
      titulo: rota.nomeMotorista ?? `Motorista #${rota.codigoMotorista}`,
      contexto: `${rota.veiculoPlaca ?? `#${rota.codigoVeiculo}`} · ${rota.enderecoPartida} → ${rota.enderecoChegada}`,
      detalhe: `aberta há ${dias} dias`,
      destino: '/rotas',
      acao: 'Rotas',
    })
  }

  for (const v of custoDoMes?.porVeiculo ?? []) {
    if (v.km <= 0 || v.totalAbastecimento > 0) continue
    pendencias.push({
      chave: `abastecimento-${v.veiculoId}`,
      tom: 'warning',
      rotulo: 'Sem abastecimento',
      titulo: v.veiculoPlaca,
      contexto: `${v.veiculoNome} · rodou ${formatKm(v.km)} no mês`,
      detalhe: 'nenhum abastecimento lançado',
      destino: '/abastecimentos',
      acao: 'Abastecimentos',
    })
  }

  const semCusto = custoDoMes?.manutencoesSemCustoInformado ?? 0
  if (semCusto > 0) {
    pendencias.push({
      chave: 'manutencao-sem-custo',
      tom: 'warning',
      rotulo: 'Sem custo',
      titulo: plural(semCusto, 'manutenção concluída', 'manutenções concluídas'),
      contexto: 'fora do custo do mês',
      detalhe: 'custo não informado',
      destino: '/manutencoes',
      acao: 'Manutenções',
    })
  }

  return pendencias
}

function FaixaDeKpis({ kpis }: { kpis: Kpi[] }) {
  return (
    <div
      className="mb-8 grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-6"
      style={{ border: '1px solid var(--color-divider)' }}
    >
      {kpis.map((kpi) => (
        <div
          key={kpi.label}
          className="flex flex-col gap-2 p-5"
          style={{ borderRight: '1px solid var(--color-divider)', background: 'var(--color-surface)' }}
          title={kpi.title}
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
            className="text-[26px] leading-none font-extrabold"
            style={{ fontFamily: 'var(--font-heading)', color: kpi.cor }}
          >
            {kpi.value}
          </span>
          <span className="text-xs" style={{ color: 'var(--color-accent-700)' }}>
            {kpi.detail}
          </span>
        </div>
      ))}
    </div>
  )
}

function Unidade({ children }: { children: ReactNode }) {
  return <span className="text-base font-semibold">{children}</span>
}

function PrecisaDeAtencao({
  pendencias,
  carregando,
  erro,
}: {
  pendencias: Pendencia[]
  carregando: boolean
  erro: boolean
}) {
  return (
    <section className="mb-8" aria-labelledby="precisaDeAtencao">
      <div className="mb-3 flex items-baseline justify-between gap-3">
        <h3 id="precisaDeAtencao" style={{ margin: 0 }}>
          Precisa de atenção
        </h3>
        {!carregando && pendencias.length > 0 && (
          <span className="text-[13px]" style={{ color: mutedText }}>
            {plural(pendencias.length, 'item', 'itens')}
          </span>
        )}
      </div>

      {erro && <ErrorList mensagens={['Parte das pendências não pôde ser verificada. Tente atualizar.']} />}

      <div style={{ borderTop: '2px solid var(--color-divider)' }}>
        {carregando && (
          <p className="m-0 px-2 py-4 text-[13px]" style={{ color: mutedText }}>
            Verificando pendências…
          </p>
        )}

        {!carregando && !erro && pendencias.length === 0 && (
          <div
            className="flex flex-wrap items-center gap-4 px-2 py-4"
            style={{ borderBottom: '1px solid var(--color-divider)' }}
          >
            <span className="tag tag-success">Tudo em dia</span>
            <span className="text-sm" style={{ color: mutedText }}>
              Nenhuma manutenção vencida, nenhuma rota esquecida e nenhum lançamento faltando.
            </span>
          </div>
        )}

        {!carregando &&
          pendencias.map((p) => (
            <div
              key={p.chave}
              className="grid grid-cols-1 gap-1 px-2 py-3 md:grid-cols-[150px_minmax(0,1fr)_220px_120px] md:items-center md:gap-4"
              style={{ borderBottom: '1px solid var(--color-divider)' }}
            >
              <span>
                <span className={`tag tag-${p.tom}`}>{p.rotulo}</span>
              </span>
              <span className="text-sm">
                <strong className="font-semibold">{p.titulo}</strong>{' '}
                <span style={{ color: mutedText }}>· {p.contexto}</span>
              </span>
              <span
                className="text-[13px] font-semibold"
                style={{ color: p.tom === 'danger' ? 'var(--color-danger)' : 'var(--color-warning)' }}
              >
                {p.detalhe}
              </span>
              <Link to={p.destino} className="text-[13px] font-semibold md:justify-self-end">
                {p.acao} →
              </Link>
            </div>
          ))}
      </div>
    </section>
  )
}

function RotasEmAndamento({
  rotas,
  total,
  pending,
  error,
}: {
  rotas: RotaResponse[]
  total: number
  pending: boolean
  error: unknown
}) {
  return (
    <section aria-labelledby="rotasEmAndamento">
      <div className="mb-3 flex items-baseline justify-between gap-3">
        <h4 id="rotasEmAndamento" style={{ margin: 0 }}>
          Rotas em andamento
        </h4>
        <Link to="/rotas" className="text-[13px] font-semibold">
          {total > rotas.length ? `Ver todas (${total}) →` : 'Ver rotas →'}
        </Link>
      </div>
      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Motorista · veículo</th>
              <th>Trajeto</th>
              <th>Desde</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={3}
              pending={pending}
              error={error}
              empty={!pending && !error && rotas.length === 0}
              textoCarregando="Carregando rotas…"
              textoErro="Não foi possível carregar as rotas."
              textoVazio="Nenhuma rota em andamento."
            />
            {rotas.map((rota) => {
              const dias = diasEmAberto(rota)
              return (
                <tr key={rota.id}>
                  <td>
                    <div className="font-semibold">{rota.nomeMotorista ?? `#${rota.codigoMotorista}`}</div>
                    <div className="text-xs" style={{ color: mutedText }}>
                      {rota.veiculoPlaca ?? `#${rota.codigoVeiculo}`}
                    </div>
                  </td>
                  <td>
                    {rota.enderecoPartida} <span style={{ color: mutedText }}>→</span> {rota.enderecoChegada}
                    {rota.dataCalculoRota && (
                      <div className="text-xs" style={{ color: mutedText }}>
                        {formatDistancia(rota.distanciaMetros)} · {formatDuracao(rota.duracaoEstimadaSegundos)}
                      </div>
                    )}
                  </td>
                  <td className="whitespace-nowrap">
                    {formatDate(rota.dataInicio)}
                    {dias >= DIAS_ALERTA_ROTA_ABERTA && (
                      <div className="mt-1">
                        <span className="tag tag-warning">Há {dias} dias</span>
                      </div>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </section>
  )
}

function VeiculosQueMaisCustaram({
  veiculos,
  pending,
  error,
}: {
  veiculos: CustoPorVeiculoResponse[]
  pending: boolean
  error: unknown
}) {
  const maior = veiculos[0]?.total ?? 0

  return (
    <section aria-labelledby="veiculosQueMaisCustaram">
      <div className="mb-3 flex items-baseline justify-between gap-3">
        <h4 id="veiculosQueMaisCustaram" style={{ margin: 0 }}>
          Veículos que mais custaram no mês
        </h4>
        <Link to="/custos" className="text-[13px] font-semibold">
          Ver em custos →
        </Link>
      </div>
      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Veículo</th>
              <th style={{ width: '45%' }}>Total</th>
              <th style={{ textAlign: 'right' }}>Custo por km</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={3}
              pending={pending}
              error={error}
              empty={!pending && !error && veiculos.length === 0}
              textoCarregando="Carregando custos…"
              textoErro="Não foi possível carregar os custos."
              textoVazio="Nenhum custo lançado no mês."
            />
            {veiculos.map((v) => (
              <tr key={v.veiculoId}>
                <td>
                  <div className="font-semibold">{v.veiculoPlaca}</div>
                  <div className="text-xs" style={{ color: mutedText }}>
                    {v.veiculoNome}
                  </div>
                </td>
                <td>
                  <div className="flex items-center gap-2.5">
                    <div className="h-2 flex-1" style={{ background: 'var(--color-neutral-200)' }}>
                      <div
                        className="h-2"
                        style={{
                          width: `${maior > 0 ? (v.total / maior) * 100 : 0}%`,
                          background: 'var(--color-accent-400)',
                        }}
                      />
                    </div>
                    <span className="font-semibold whitespace-nowrap tabular-nums">{formatMoeda(v.total)}</span>
                  </div>
                </td>
                <td className="tabular-nums" style={{ textAlign: 'right' }}>
                  {formatCustoPorKm(v.custoPorKm)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export function DashboardPage() {
  const hoje = new Date()
  const mes = mesAteHoje(hoje)
  const comparacao = mesmoPeriodoDoMesPassado(hoje)
  const semestre = ultimosMeses(6, hoje)

  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })

  const rotasAtivasQuery = useQuery({
    queryKey: ['rotas', { ativo: true, tamanhoPagina: 100 }],
    queryFn: () => rotasApi.getAll({ ativo: true, pagina: 1, tamanhoPagina: 100 }),
  })

  const kmDoMesQuery = useQuery({
    queryKey: ['rotas', 'resumo', mes],
    queryFn: () => rotasApi.resumo(mes.de, mes.ate),
  })

  const custoDoMesQuery = useQuery({
    queryKey: ['custos', 'resumo', mes],
    queryFn: () => custosApi.resumo(mes),
  })

  const custoComparacaoQuery = useQuery({
    queryKey: ['custos', 'resumo', comparacao],
    queryFn: () => custosApi.resumo(comparacao),
  })

  const custoSemestreQuery = useQuery({
    queryKey: ['custos', 'resumo', semestre],
    queryFn: () => custosApi.resumo(semestre),
  })

  const manutencoesQuery = useQuery({
    queryKey: ['manutencoes', 'resumo'],
    queryFn: manutencoesApi.resumo,
  })

  const consultas = [
    veiculosQuery,
    rotasAtivasQuery,
    kmDoMesQuery,
    custoDoMesQuery,
    custoComparacaoQuery,
    custoSemestreQuery,
    manutencoesQuery,
  ]
  const atualizando = consultas.some((q) => q.isFetching)
  const atualizadoEm = Math.max(...consultas.map((q) => q.dataUpdatedAt))

  const veiculos = veiculosQuery.data ?? []
  const emRota = veiculos.filter((v) => v.emRota).length
  const rotasAtivas = rotasAtivasQuery.data?.itens ?? []
  const custoDoMes = custoDoMesQuery.data
  const custoComparacao = custoComparacaoQuery.data
  const kmDoMes = kmDoMesQuery.data
  const manutencoes = manutencoesQuery.data

  const kpis: Kpi[] = [
    {
      label: 'Frota agora',
      value: veiculosQuery.isSuccess ? (
        <>
          {emRota} <Unidade>/ {veiculos.length}</Unidade>
        </>
      ) : (
        '—'
      ),
      detail: veiculosQuery.isSuccess ? `em rota · ${veiculos.length - emRota} disponíveis` : '',
      icon: <TruckIcon size={16} />,
    },
    {
      label: 'Custo do mês',
      value: custoDoMes ? formatMoedaCompacta(custoDoMes.total) : '—',
      title: custoDoMes ? formatMoeda(custoDoMes.total) : undefined,
      detail: custoDoMes && custoComparacao ? variacaoDoCusto(custoDoMes, custoComparacao, comparacao) : '',
      icon: <DinheiroIcon size={16} />,
    },
    {
      label: 'Km rodado',
      value: kmDoMes ? kmDoMes.kmTotal.toLocaleString('pt-BR') : '—',
      detail: kmDoMes ? `no mês · ${plural(kmDoMes.quantidade, 'rota encerrada', 'rotas encerradas')}` : '',
      icon: <RouteIcon size={16} />,
    },
    {
      label: 'Custo por km',
      value: custoDoMes ? formatCustoPorKm(custoDoMes.custoPorKm).replace('/km', '') : '—',
      detail: custoDoMes ? `${formatKm(custoDoMes.kmTotal)} em rotas encerradas` : '',
      icon: <RouteIcon size={16} />,
    },
    {
      label: 'Consumo médio',
      value: custoDoMes ? formatConsumo(custoDoMes.consumoMedio) : '—',
      detail: custoDoMes ? `${formatKm(custoDoMes.kmOdometroTotal)} pelo odômetro` : '',
      icon: <FuelIcon size={16} />,
    },
    {
      label: 'Manutenção',
      value: manutencoes ? (
        <>
          {manutencoes.atrasadas} <Unidade>{manutencoes.atrasadas === 1 ? 'atrasada' : 'atrasadas'}</Unidade>
        </>
      ) : (
        '—'
      ),
      cor: manutencoes && manutencoes.atrasadas > 0 ? 'var(--color-danger)' : undefined,
      detail: manutencoes ? `${manutencoes.vencendo} vencendo em até ${formatKm(FAIXA_AVISO)}` : '',
      icon: <WrenchIcon size={16} />,
    },
  ]

  const pendencias = montarPendencias(manutencoes, rotasAtivas, custoDoMes)
  const ranking = (custoDoMes?.porVeiculo ?? []).filter((v) => v.total > 0).slice(0, MAXIMO_RANKING)

  const subtitulo = atualizadoEm
    ? `${nomeDoMes(hoje, true).replace(/^./, (c) => c.toUpperCase())} · atualizado às ${new Date(atualizadoEm).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`
    : 'Carregando dados…'

  return (
    <AppLayout>
      <PageHeader
        titulo="Visão geral da frota"
        subtitulo={subtitulo}
        acoes={
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => consultas.forEach((q) => q.refetch())}
            disabled={atualizando}
          >
            {atualizando ? 'Atualizando…' : 'Atualizar'}
          </button>
        }
      />

      <FaixaDeKpis kpis={kpis} />

      <PrecisaDeAtencao
        pendencias={pendencias}
        carregando={manutencoesQuery.isPending || rotasAtivasQuery.isPending || custoDoMesQuery.isPending}
        erro={manutencoesQuery.isError || rotasAtivasQuery.isError || custoDoMesQuery.isError}
      />

      <div className="mb-8 grid grid-cols-1 gap-8 xl:grid-cols-2">
        <RotasEmAndamento
          rotas={rotasAtivas.slice(0, MAXIMO_ROTAS_EM_ANDAMENTO)}
          total={rotasAtivasQuery.data?.total ?? 0}
          pending={rotasAtivasQuery.isPending}
          error={rotasAtivasQuery.error}
        />
        <VeiculosQueMaisCustaram
          veiculos={ranking}
          pending={custoDoMesQuery.isPending}
          error={custoDoMesQuery.error}
        />
      </div>

      {custoSemestreQuery.isSuccess && (
        <PainelCustoMensal
          titulo="Custo dos últimos 6 meses"
          meses={preencherMeses(custoSemestreQuery.data.porMes, semestre)}
          comparacaoParcial={
            custoComparacao ? { total: custoComparacao.total, atual: mes, anterior: comparacao } : undefined
          }
          atualizando={custoSemestreQuery.isFetching}
          textoVazio="Nenhum custo lançado nos últimos 6 meses. Os abastecimentos, manutenções concluídas e despesas aparecem aqui conforme forem lançados."
        />
      )}
    </AppLayout>
  )
}
