import { useState, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { custosApi } from '../api/custos'
import { motoristasApi } from '../api/motoristas'
import { veiculosApi } from '../api/veiculos'
import type {
  CustoFiltro,
  CustoPorMesResponse,
  CustoPorVeiculoResponse,
  LancamentoCustoResponse,
  MotoristaResponse,
  OrigemCusto,
  ResumoCustosResponse,
  VeiculoResponse,
} from '../api/types'
import { AppLayout, PageHeader } from '../components/AppLayout'
import { FiltroPeriodo, PainelDialog, Paginacao, TableStates } from '../components/Table'
import { useTamanhoPagina } from '../lib/paginacao'
import { DinheiroIcon, FuelIcon, ReciboIcon, RouteIcon, WrenchIcon } from '../components/icons'
import { formatDate, formatKm, formatMoeda } from '../lib/format'
import { formatConsumo, formatCustoPorKm, rotuloDoMes, ROTULO_ORIGEM } from '../lib/custo'
import { intervaloDoPeriodo, type Periodo } from '../lib/periodo'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

/** As três séries do gráfico saem da rampa do acento: são categorias, não situações. */
const COR_ABASTECIMENTO = 'var(--color-accent-700)'
const COR_MANUTENCAO = 'var(--color-accent-400)'
const COR_DESPESA = 'var(--color-accent-200)'

function FiltrosCustos({
  veiculos,
  motoristas,
  filtroVeiculo,
  filtroMotorista,
  filtroOrigem,
  periodo,
  temFiltro,
  onFiltroVeiculoChange,
  onFiltroMotoristaChange,
  onFiltroOrigemChange,
  onPeriodoChange,
  onLimpar,
}: {
  veiculos: VeiculoResponse[]
  motoristas: MotoristaResponse[]
  filtroVeiculo: string
  filtroMotorista: string
  filtroOrigem: string
  periodo: Periodo
  temFiltro: boolean
  onFiltroVeiculoChange: (valor: string) => void
  onFiltroMotoristaChange: (valor: string) => void
  onFiltroOrigemChange: (valor: string) => void
  onPeriodoChange: (valor: Periodo) => void
  onLimpar: () => void
}) {
  return (
    <div className="mb-5 flex flex-wrap items-end gap-4">
      <div className="field w-[230px]">
        <label htmlFor="filtroVeiculoCusto">Veículo</label>
        <select
          id="filtroVeiculoCusto"
          className="input"
          style={{ borderRadius: 0 }}
          value={filtroVeiculo}
          onChange={(e) => onFiltroVeiculoChange(e.target.value)}
        >
          <option value="">Toda a frota</option>
          {veiculos.map((v) => (
            <option key={v.id} value={v.id}>
              {v.placa} — {v.nomeVeiculo}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[230px]">
        <label htmlFor="filtroMotoristaCusto">Motorista</label>
        <select
          id="filtroMotoristaCusto"
          className="input"
          style={{ borderRadius: 0 }}
          value={filtroMotorista}
          onChange={(e) => onFiltroMotoristaChange(e.target.value)}
        >
          <option value="">Todos os motoristas</option>
          {motoristas.map((m) => (
            <option key={m.id} value={m.id}>
              {m.nome}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[190px]">
        <label htmlFor="filtroOrigemCusto">Origem</label>
        <select
          id="filtroOrigemCusto"
          className="input"
          style={{ borderRadius: 0 }}
          value={filtroOrigem}
          onChange={(e) => onFiltroOrigemChange(e.target.value)}
        >
          <option value="">Todas as origens</option>
          {(Object.keys(ROTULO_ORIGEM) as OrigemCusto[]).map((origem) => (
            <option key={origem} value={origem}>
              {ROTULO_ORIGEM[origem]}
            </option>
          ))}
        </select>
      </div>

      <FiltroPeriodo valor={periodo} onMudar={onPeriodoChange} id="filtroPeriodoCusto" />

      {temFiltro && (
        <button
          type="button"
          className="btn btn-secondary"
          style={{ borderRadius: 0, padding: '10px 18px' }}
          onClick={onLimpar}
        >
          Limpar filtros
        </button>
      )}
    </div>
  )
}

function FaixaDeKpis({ resumo, carregado }: { resumo?: ResumoCustosResponse; carregado: boolean }) {
  const kpis: { label: string; value: string; detail: string; icon: ReactNode }[] = [
    {
      label: 'Custo total',
      value: carregado && resumo ? formatMoeda(resumo.total) : '—',
      detail:
        carregado && resumo
          ? `${resumo.quantidadeLancamentos} ${resumo.quantidadeLancamentos === 1 ? 'lançamento' : 'lançamentos'}`
          : '',
      icon: <DinheiroIcon size={16} />,
    },
    {
      label: 'Combustível',
      value: carregado && resumo ? formatMoeda(resumo.totalAbastecimento) : '—',
      detail: carregado && resumo ? participacao(resumo.totalAbastecimento, resumo.total) : '',
      icon: <FuelIcon size={16} />,
    },
    {
      label: 'Manutenção',
      value: carregado && resumo ? formatMoeda(resumo.totalManutencao) : '—',
      detail: carregado && resumo ? participacao(resumo.totalManutencao, resumo.total) : '',
      icon: <WrenchIcon size={16} />,
    },
    {
      label: 'Despesas',
      value: carregado && resumo ? formatMoeda(resumo.totalDespesa) : '—',
      detail: carregado && resumo ? participacao(resumo.totalDespesa, resumo.total) : '',
      icon: <ReciboIcon size={16} />,
    },
    {
      label: 'Custo por km',
      value: carregado && resumo ? formatCustoPorKm(resumo.custoPorKm) : '—',
      detail: carregado && resumo ? `${formatKm(resumo.kmTotal)} em rotas encerradas` : '',
      icon: <RouteIcon size={16} />,
    },
    {
      // O detalhe não é enfeite: este km e o do KPI ao lado são medidas diferentes, e sem
      // dizer de onde cada um vem a tela pareceria estar se contradizendo.
      label: 'Consumo médio',
      value: carregado && resumo ? formatConsumo(resumo.consumoMedio) : '—',
      detail: carregado && resumo ? `${formatKm(resumo.kmOdometroTotal)} pelo odômetro` : '',
      icon: <FuelIcon size={16} />,
    },
  ]

  return (
    <div className="mb-6 grid grid-cols-2 lg:grid-cols-3" style={{ border: '1px solid var(--color-divider)' }}>
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
          <span className="text-[26px] leading-none font-extrabold" style={{ fontFamily: 'var(--font-heading)' }}>
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

function participacao(parte: number, total: number): string {
  if (total <= 0) return 'sem gasto no período'
  return `${Math.round((parte / total) * 100)}% do total`
}

/**
 * Barras em CSS puro. Uma biblioteca de gráfico seria a primeira dependência de front do
 * projeto para desenhar três retângulos empilhados — não se paga.
 */
function GraficoPorMes({ meses }: { meses: CustoPorMesResponse[] }) {
  if (meses.length < 2) return null

  const maximo = Math.max(...meses.map((m) => m.total))
  if (maximo <= 0) return null

  return (
    <section className="mb-8">
      <div className="mb-3 flex flex-wrap items-end justify-between gap-3">
        <h3 style={{ margin: 0 }}>Evolução mensal</h3>
        <div className="flex items-center gap-4 text-xs" style={{ color: mutedText }}>
          <span className="flex items-center gap-1.5">
            <span style={{ width: 10, height: 10, background: COR_ABASTECIMENTO }} />
            Combustível
          </span>
          <span className="flex items-center gap-1.5">
            <span style={{ width: 10, height: 10, background: COR_MANUTENCAO }} />
            Manutenção
          </span>
          <span className="flex items-center gap-1.5">
            <span style={{ width: 10, height: 10, background: COR_DESPESA }} />
            Despesas
          </span>
        </div>
      </div>

      <div
        className="flex items-end gap-3 overflow-x-auto p-4"
        style={{ border: '1px solid var(--color-divider)', background: 'var(--color-surface)' }}
      >
        {meses.map((mes) => (
          <div key={`${mes.ano}-${mes.mes}`} className="flex min-w-[52px] flex-1 flex-col items-center gap-2">
            <span className="text-[11px] font-semibold">{formatMoeda(mes.total)}</span>
            <div
              className="flex w-full flex-col justify-end"
              style={{ height: 140 }}
              title={`${rotuloDoMes(mes.ano, mes.mes)}: ${formatMoeda(mes.total)}`}
            >
              <div
                style={{ height: `${(mes.totalDespesa / maximo) * 100}%`, background: COR_DESPESA }}
              />
              <div
                style={{ height: `${(mes.totalManutencao / maximo) * 100}%`, background: COR_MANUTENCAO }}
              />
              <div
                style={{ height: `${(mes.totalAbastecimento / maximo) * 100}%`, background: COR_ABASTECIMENTO }}
              />
            </div>
            <span className="text-[11px]" style={{ color: mutedText }}>
              {rotuloDoMes(mes.ano, mes.mes)}
            </span>
          </div>
        ))}
      </div>
    </section>
  )
}

function TabelaPorVeiculo({
  veiculos,
  pending,
  error,
  onVerLancamentos,
}: {
  veiculos: CustoPorVeiculoResponse[]
  pending: boolean
  error: unknown
  onVerLancamentos: (veiculo: CustoPorVeiculoResponse) => void
}) {
  return (
    <section className="mb-8">
      <h3 className="mb-3" style={{ margin: '0 0 12px' }}>
        Por veículo
      </h3>
      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Veículo</th>
              <th>Combustível</th>
              <th>Manutenção</th>
              <th>Despesas</th>
              <th>Total</th>
              <th>Km rodado</th>
              <th>Custo por km</th>
              <th>Consumo</th>
              <th style={{ textAlign: 'right' }}>Lançamentos</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={9}
              pending={pending}
              error={error}
              empty={veiculos.length === 0}
              textoCarregando="Somando custos…"
              textoErro="Não foi possível carregar o resumo."
              textoVazio="Nenhum custo lançado no período escolhido."
            />
            {veiculos.map((v) => (
              <tr key={v.veiculoId}>
                <td className="font-semibold">
                  {v.veiculoPlaca}
                  <div className="text-xs font-normal" style={{ color: mutedText }}>
                    {v.veiculoNome}
                  </div>
                </td>
                <td>{formatMoeda(v.totalAbastecimento)}</td>
                <td>{formatMoeda(v.totalManutencao)}</td>
                <td>{formatMoeda(v.totalDespesa)}</td>
                <td className="font-semibold">{formatMoeda(v.total)}</td>
                <td>{v.km > 0 ? formatKm(v.km) : '—'}</td>
                <td>{formatCustoPorKm(v.custoPorKm)}</td>
                <td>{formatConsumo(v.consumoMedio)}</td>
                <td style={{ textAlign: 'right' }}>
                  {/* Botão de texto, e não um ícone de `RowActions`: isto não é editar nem
                      excluir — é abrir o detalhe que sustenta os números da linha. */}
                  <button
                    type="button"
                    className="btn btn-secondary"
                    style={{ padding: '4px 12px', fontSize: 13 }}
                    onClick={() => onVerLancamentos(v)}
                  >
                    Ver
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

function TabelaLancamentos({
  lancamentos,
  pending,
  error,
  temFiltro,
}: {
  lancamentos: LancamentoCustoResponse[]
  pending: boolean
  error: unknown
  temFiltro: boolean
}) {
  return (
    <div className="overflow-x-auto">
      <table className="table">
        <thead>
          <tr>
            <th>Data</th>
            <th>Origem</th>
            <th>Veículo</th>
            <th>Motorista</th>
            <th>Categoria</th>
            <th>Valor</th>
            <th>Observação</th>
          </tr>
        </thead>
        <tbody>
          <TableStates
            colSpan={7}
            pending={pending}
            error={error}
            empty={lancamentos.length === 0}
            textoCarregando="Carregando lançamentos…"
            textoErro="Não foi possível carregar os custos."
            textoVazio={
              temFiltro
                ? 'Nenhum custo para os filtros escolhidos.'
                : 'Nenhum custo lançado ainda. Eles aparecem aqui conforme os abastecimentos e as manutenções concluídas.'
            }
          />
          {lancamentos.map((l) => (
            <tr key={`${l.origem}-${l.origemId}`}>
              <td>{formatDate(l.data)}</td>
              <td>{ROTULO_ORIGEM[l.origem]}</td>
              <td className="font-semibold">
                {l.veiculoPlaca}
                <div className="text-xs font-normal" style={{ color: mutedText }}>
                  {l.veiculoNome}
                </div>
              </td>
              <td>{l.motoristaNome ?? '—'}</td>
              <td>{l.categoria}</td>
              <td className="font-semibold">{formatMoeda(l.valor)}</td>
              <td style={{ color: mutedText }}>{l.observacao || '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/**
 * Os lançamentos de um veículo, sob demanda. Saíram da tela principal porque competiam com
 * o resumo pela atenção: quem abre `/custos` quer o total, e só desce ao lançamento quando
 * um número não fecha.
 *
 * O `recorte` que chega é o mesmo da tela (período, origem, motorista) — o modal só
 * acrescenta o veículo. Assim o que se vê aqui explica exatamente a linha que foi clicada.
 */
function LancamentosDoVeiculoDialog({
  veiculo,
  recorte,
  temFiltro,
  onFechar,
}: {
  veiculo: CustoPorVeiculoResponse
  recorte: Omit<CustoFiltro, 'pagina' | 'tamanhoPagina' | 'veiculoId'>
  temFiltro: boolean
  onFechar: () => void
}) {
  const [pagina, setPagina] = useState(1)
  const { tamanhoPagina, setTamanhoPagina } = useTamanhoPagina()

  const filtro: CustoFiltro = {
    ...recorte,
    veiculoId: veiculo.veiculoId,
    pagina,
    tamanhoPagina,
  }

  // Mesmo prefixo `['custos']` das outras consultas — a invalidação de abastecimento,
  // manutenção, despesa e encerramento de rota já alcança este modal sem nada novo.
  const query = useQuery({
    queryKey: ['custos', filtro],
    queryFn: () => custosApi.consultar(filtro),
  })

  const dados = query.data

  return (
    <PainelDialog
      titulo={`Lançamentos — ${veiculo.veiculoPlaca}`}
      descricao={`${veiculo.veiculoNome} · ${formatMoeda(veiculo.total)} no recorte atual da tela.`}
      largura={900}
      onFechar={onFechar}
    >
      <TabelaLancamentos
        lancamentos={dados?.itens ?? []}
        pending={query.isPending}
        error={query.error}
        temFiltro={temFiltro}
      />

      {dados && (
        <Paginacao
          pagina={dados.pagina}
          totalPaginas={dados.totalPaginas}
          total={dados.total}
          tamanhoPagina={dados.tamanhoPagina}
          onMudar={setPagina}
          onMudarTamanho={(t) => {
            setTamanhoPagina(t)
            setPagina(1)
          }}
          pending={query.isFetching}
        />
      )}
    </PainelDialog>
  )
}

export function CustosPage() {
  /** O veículo cujo detalhe está aberto. Nulo = tela de resumo, sem modal. */
  const [lancamentosDe, setLancamentosDe] = useState<CustoPorVeiculoResponse | null>(null)
  const [filtroVeiculo, setFiltroVeiculo] = useState('')
  const [filtroMotorista, setFiltroMotorista] = useState('')
  const [filtroOrigem, setFiltroOrigem] = useState('')
  // Começa no mês corrente, não em "todo o período": um total de todos os tempos não
  // responde pergunta nenhuma e ainda faz a primeira carga ser a mais cara possível.
  const [periodo, setPeriodo] = useState<Periodo>('esteMes')

  const intervalo = intervaloDoPeriodo(periodo)

  /**
   * O recorte da tela. Alimenta o resumo e é repassado ao modal de lançamentos, que só
   * acrescenta o veículo e a paginação.
   */
  const recorte = {
    veiculoId: filtroVeiculo === '' ? undefined : Number(filtroVeiculo),
    motoristaId: filtroMotorista === '' ? undefined : Number(filtroMotorista),
    origem: filtroOrigem === '' ? undefined : (filtroOrigem as OrigemCusto),
    de: intervalo.de,
    ate: intervalo.ate,
  }

  const resumoQuery = useQuery({
    queryKey: ['custos', 'resumo', recorte],
    queryFn: () => custosApi.resumo(recorte),
  })

  // A tela é de gestão, então os dois endpoints estão sempre disponíveis aqui.
  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })
  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })

  const resumo = resumoQuery.data

  const temFiltro = filtroVeiculo !== '' || filtroMotorista !== '' || filtroOrigem !== '' || periodo !== 'todos'
  const atualizando = resumoQuery.isFetching

  /**
   * Mudar filtro fecha o detalhe aberto: o modal mostra o recorte da tela, e mantê-lo
   * aberto sobre um recorte que mudou embaixo dele diria uma coisa e mostraria outra.
   */
  function fecharDetalhe() {
    setLancamentosDe(null)
  }

  function limparFiltros() {
    setFiltroVeiculo('')
    setFiltroMotorista('')
    setFiltroOrigem('')
    setPeriodo('todos')
    fecharDetalhe()
  }

  return (
    <AppLayout>
      <PageHeader
        titulo="Custos"
        subtitulo='Abastecimentos, manutenções concluídas e despesas em uma visão só. O lançamento continua sendo feito nas telas de origem; para ver os de um veículo, use "Ver" na linha dele.'
        acoes={
          <button
            type="button"
            className="btn btn-secondary"
            style={{ borderRadius: 0 }}
            onClick={() => resumoQuery.refetch()}
            disabled={atualizando}
          >
            {atualizando ? 'Atualizando…' : 'Atualizar'}
          </button>
        }
      />

      <FiltrosCustos
        veiculos={veiculosQuery.data ?? []}
        motoristas={motoristasQuery.data ?? []}
        filtroVeiculo={filtroVeiculo}
        filtroMotorista={filtroMotorista}
        filtroOrigem={filtroOrigem}
        periodo={periodo}
        temFiltro={temFiltro}
        onFiltroVeiculoChange={(valor) => {
          setFiltroVeiculo(valor)
          fecharDetalhe()
        }}
        onFiltroMotoristaChange={(valor) => {
          setFiltroMotorista(valor)
          fecharDetalhe()
        }}
        onFiltroOrigemChange={(valor) => {
          setFiltroOrigem(valor)
          fecharDetalhe()
        }}
        onPeriodoChange={(valor) => {
          setPeriodo(valor)
          fecharDetalhe()
        }}
        onLimpar={limparFiltros}
      />

      <FaixaDeKpis resumo={resumo} carregado={resumoQuery.isSuccess} />

      {/* Os dois avisos existem porque, sem eles, o número da tela mente. */}
      {filtroMotorista !== '' && (
        <p className="mb-4 flex flex-wrap gap-2">
          <span className="tag tag-warning">
            Manutenção não é atribuída a motorista — este recorte mostra abastecimentos e despesas.
          </span>
          {/* O consumo é o que mais sofre com o recorte: o odômetro salta os abastecimentos
              dos outros motoristas, então o km cobre trechos que este não pagou. */}
          <span className="tag tag-warning">
            O consumo fica superestimado por motorista — o odômetro conta trechos abastecidos por
            outras pessoas.
          </span>
        </p>
      )}

      {resumo !== undefined && resumo.manutencoesSemCustoInformado > 0 && (
        <p className="mb-4">
          <span className="tag tag-warning">
            {resumo.manutencoesSemCustoInformado}{' '}
            {resumo.manutencoesSemCustoInformado === 1
              ? 'manutenção concluída sem custo informado não entra'
              : 'manutenções concluídas sem custo informado não entram'}{' '}
            neste total.
          </span>
        </p>
      )}

      <GraficoPorMes meses={resumo?.porMes ?? []} />

      <TabelaPorVeiculo
        veiculos={resumo?.porVeiculo ?? []}
        pending={resumoQuery.isPending}
        error={resumoQuery.error}
        onVerLancamentos={setLancamentosDe}
      />

      {lancamentosDe && (
        <LancamentosDoVeiculoDialog
          veiculo={lancamentosDe}
          recorte={recorte}
          temFiltro={temFiltro}
          onFechar={fecharDetalhe}
        />
      )}
    </AppLayout>
  )
}
