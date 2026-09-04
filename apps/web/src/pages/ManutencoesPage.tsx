import { useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { manutencoesApi } from '../api/manutencoes'
import { tiposManutencaoApi } from '../api/tiposManutencao'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import type {
  ConcluirManutencaoRequest,
  ManutencaoFiltro,
  ManutencaoRequest,
  ManutencaoResponse,
  StatusManutencao,
  TipoManutencaoResponse,
  VeiculoResponse,
} from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, PageHeader } from '../components/AppLayout'
import {
  ConfirmDialog,
  FiltroPeriodo,
  FormDialog,
  Paginacao,
  RowActions,
  SecaoCampos,
  TableStates,
} from '../components/Table'
import { usePaginacao } from '../lib/paginacao'
import { CheckIcon } from '../components/icons'
import { formatDate, formatKm, formatMoeda, hojeInputDate, paraInputDate } from '../lib/format'
import { badgeDaManutencao, estaVencendo, textoKmRestantes } from '../lib/manutencao'
import { intervaloDoPeriodo, type Periodo } from '../lib/periodo'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = {
  veiculoId: '',
  tipoManutencaoId: '',
  quilometragemPrevista: '',
  dataPrevista: '',
  observacao: '',
}

const CONCLUSAO_VAZIA = {
  quilometragemRealizada: '',
  dataRealizacao: '',
  custo: '',
  observacao: '',
}

type FormularioManutencao = typeof FORM_VAZIO
type FormularioConclusao = typeof CONCLUSAO_VAZIA

/** Modal de agendamento/edição — o mesmo formulário para as duas ações, o id decide o verbo. */
function ManutencaoFormulario({
  editando,
  form,
  onFormChange,
  onAplicarSelecao,
  onSubmit,
  onCancelar,
  pending,
  erros,
  veiculos,
  tipos,
}: {
  editando: ManutencaoResponse | null
  form: FormularioManutencao
  onFormChange: (form: FormularioManutencao) => void
  onAplicarSelecao: (veiculoId: string, tipoId: string) => void
  onSubmit: (e: FormEvent) => void
  onCancelar: () => void
  pending: boolean
  erros: string[]
  veiculos: VeiculoResponse[]
  tipos: TipoManutencaoResponse[]
}) {
  return (
    <FormDialog
      titulo={editando ? 'Editar manutenção' : 'Nova manutenção'}
      descricao={
        editando
          ? `Editando a manutenção de ${editando.tipoManutencaoNome} do veículo ${editando.veiculoPlaca}.`
          : undefined
      }
      textoConfirmar={editando ? 'Salvar alterações' : 'Agendar'}
      textoPendente="Salvando…"
      largura={760}
      pending={pending}
      erros={erros}
      onSubmit={onSubmit}
      onCancelar={onCancelar}
    >
      <SecaoCampos titulo="Manutenção">
        <div className="field">
          <label htmlFor="veiculoId">Veículo</label>
          <select
            id="veiculoId"
            className="input"
            required
            value={form.veiculoId}
            onChange={(e) => onAplicarSelecao(e.target.value, form.tipoManutencaoId)}
          >
            <option value="">Selecione…</option>
            {veiculos.map((v) => (
              <option key={v.id} value={v.id}>
                {v.placa} — {v.nomeVeiculo} ({formatKm(v.quilometragem)})
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="tipoManutencaoId">Tipo</label>
          <select
            id="tipoManutencaoId"
            className="input"
            required
            value={form.tipoManutencaoId}
            onChange={(e) => onAplicarSelecao(form.veiculoId, e.target.value)}
          >
            <option value="">Selecione…</option>
            {tipos.map((t) => (
              <option key={t.id} value={t.id}>
                {t.nome}
                {t.intervaloKm ? ` (a cada ${formatKm(t.intervaloKm)})` : ''}
              </option>
            ))}
          </select>
        </div>
      </SecaoCampos>

      <SecaoCampos titulo="Vencimento">
        <div className="field">
          <label htmlFor="quilometragemPrevista">Quilometragem prevista</label>
          <input
            id="quilometragemPrevista"
            className="input"
            type="number"
            min={1}
            max={2000000}
            required
            placeholder="0"
            value={form.quilometragemPrevista}
            onChange={(e) => onFormChange({ ...form, quilometragemPrevista: e.target.value })}
          />
        </div>
        <div className="field">
          <label htmlFor="dataPrevista">Prazo (opcional)</label>
          <input
            id="dataPrevista"
            className="input"
            type="date"
            // No agendamento novo a API recusa data no passado; na edição, permite replanejar.
            min={editando ? undefined : hojeInputDate()}
            value={form.dataPrevista}
            onChange={(e) => onFormChange({ ...form, dataPrevista: e.target.value })}
          />
        </div>
        <p className="campo-largo m-0 text-[13px]" style={{ color: mutedText }}>
          A manutenção vence no que vier primeiro: a quilometragem prevista ou o prazo. Quando o
          tipo tem intervalo cadastrado, a quilometragem já vem sugerida (km atual do veículo +
          intervalo).
        </p>
      </SecaoCampos>

      <SecaoCampos titulo="Observação">
        <div className="field campo-largo">
          <label htmlFor="observacao">Observação (opcional)</label>
          <input
            id="observacao"
            className="input"
            type="text"
            maxLength={500}
            placeholder="Ex.: levar filtro sobressalente"
            value={form.observacao}
            onChange={(e) => onFormChange({ ...form, observacao: e.target.value })}
          />
        </div>
      </SecaoCampos>
    </FormDialog>
  )
}

/** Barra de filtros — veículo, situação e período, com a nota sobre qual data o período considera. */
function FiltrosManutencao({
  veiculos,
  filtroVeiculo,
  filtroStatus,
  periodo,
  temFiltro,
  onFiltroVeiculoChange,
  onFiltroStatusChange,
  onPeriodoChange,
  onLimpar,
}: {
  veiculos: VeiculoResponse[]
  filtroVeiculo: string
  filtroStatus: string
  periodo: Periodo
  temFiltro: boolean
  onFiltroVeiculoChange: (v: string) => void
  onFiltroStatusChange: (v: string) => void
  onPeriodoChange: (p: Periodo) => void
  onLimpar: () => void
}) {
  return (
    <div className="mb-5 flex flex-wrap items-end gap-4">
      <div className="field w-[230px]">
        <label htmlFor="filtroVeiculo">Filtrar por veículo</label>
        <select
          id="filtroVeiculo"
          className="input"
          style={{ borderRadius: 0 }}
          value={filtroVeiculo}
          onChange={(e) => onFiltroVeiculoChange(e.target.value)}
        >
          <option value="">Todos os veículos</option>
          {veiculos.map((v) => (
            <option key={v.id} value={v.id}>
              {v.placa} — {v.nomeVeiculo}
            </option>
          ))}
        </select>
      </div>
      <div className="field w-[190px]">
        <label htmlFor="filtroStatus">Situação</label>
        {/* "Cancelada" fica de fora: o status existe no enum, mas nada o produz ainda. */}
        <select
          id="filtroStatus"
          className="input"
          style={{ borderRadius: 0 }}
          value={filtroStatus}
          onChange={(e) => onFiltroStatusChange(e.target.value)}
        >
          <option value="">Todas</option>
          <option value="Pendente">Pendentes</option>
          <option value="Realizada">Concluídas</option>
        </select>
      </div>
      <FiltroPeriodo valor={periodo} onMudar={onPeriodoChange} />
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
      {/*
        O período incide sobre a data que importa em cada linha: prazo quando pendente,
        execução quando concluída. Dizer isso na tela evita a leitura errada de que o
        filtro olha uma data só.
      */}
      {periodo !== 'todos' && (
        <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
          O período considera a <strong>data prevista</strong> das pendentes e a{' '}
          <strong>data de realização</strong> das concluídas. Manutenção agendada só por
          quilometragem, sem data prevista, não aparece enquanto houver período.
        </p>
      )}
    </div>
  )
}

/** A tabela — situação, andamento e as ações por linha (concluir/editar/excluir). */
function TabelaManutencoes({
  manutencoes,
  colunas,
  mostrarCusto,
  mostrarAcoes,
  podeCadastrar,
  podeExcluir,
  temFiltro,
  pending,
  error,
  isSuccess,
  onConcluir,
  onEditar,
  onExcluir,
}: {
  manutencoes: ManutencaoResponse[]
  colunas: number
  mostrarCusto: boolean
  mostrarAcoes: boolean
  podeCadastrar: boolean
  podeExcluir: boolean
  temFiltro: boolean
  pending: boolean
  error: unknown
  isSuccess: boolean
  onConcluir: (m: ManutencaoResponse) => void
  onEditar: (m: ManutencaoResponse) => void
  onExcluir: (m: ManutencaoResponse) => void
}) {
  return (
    <div className="overflow-x-auto">
      <table className="table">
        <thead>
          <tr>
            <th>Veículo</th>
            <th>Tipo</th>
            <th>Quilometragem prevista</th>
            <th>Situação</th>
            <th>Andamento/Conclusão</th>
            {mostrarCusto && <th>Custo</th>}
            {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
          </tr>
        </thead>
        <tbody>
          <TableStates
            colSpan={colunas}
            pending={pending}
            error={error}
            empty={isSuccess && manutencoes.length === 0}
            textoCarregando="Carregando manutenções…"
            textoErro="Não foi possível carregar as manutenções."
            textoVazio={
              temFiltro
                ? 'Nenhuma manutenção encontrada com esses filtros.'
                : 'Nenhuma manutenção agendada ainda.'
            }
          />
          {manutencoes.map((m) => {
            const badge = badgeDaManutencao(m)
            const pendente = m.status === 'Pendente'
            const restante = textoKmRestantes(m.kmRestantes)
            const corDoAndamento = m.atrasada
              ? 'var(--color-danger)'
              : estaVencendo(m)
                ? 'var(--color-warning)'
                : null
            return (
              <tr key={m.id}>
                <td>
                  <div className="font-semibold">{m.veiculoPlaca}</div>
                  <div className="text-[12px]" style={{ color: mutedText }}>
                    {m.veiculoNome} · {formatKm(m.quilometragemAtualVeiculo)}
                  </div>
                </td>
                <td>
                  <div>{m.tipoManutencaoNome}</div>
                  {m.observacao && (
                    <div className="text-[12px]" style={{ color: mutedText }}>
                      {m.observacao}
                    </div>
                  )}
                </td>
                <td>
                  <div>{formatKm(m.quilometragemPrevista)}</div>
                  {m.dataPrevista && (
                    <div className="text-[12px]" style={{ color: mutedText }}>
                      até {formatDate(m.dataPrevista)}
                    </div>
                  )}
                </td>
                <td>
                  <span className={badge.classe}>{badge.rotulo}</span>
                </td>
                <td>
                  {pendente ? (
                    // Acompanha a cor da tag da mesma linha: vermelho no atraso,
                    // âmbar na faixa de aviso.
                    <span style={corDoAndamento ? { color: corDoAndamento } : undefined}>
                      {restante ?? '—'}
                    </span>
                  ) : m.dataRealizacao ? (
                    <div className="text-[13px]">
                      {formatDate(m.dataRealizacao)}
                      {m.quilometragemRealizada != null && (
                        <span style={{ color: mutedText }}>
                          {' '}
                          · {formatKm(m.quilometragemRealizada)}
                        </span>
                      )}
                    </div>
                  ) : (
                    '—'
                  )}
                </td>
                {mostrarCusto && <td>{formatMoeda(m.custo)}</td>}
                {mostrarAcoes && (
                  <td>
                    <div className="flex items-center justify-end gap-1">
                      {/* Editar e concluir só valem em linha pendente — o resto é histórico. */}
                      {podeCadastrar && pendente && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          style={{ borderRadius: 0, padding: '6px 12px', fontSize: 12 }}
                          onClick={() => onConcluir(m)}
                        >
                          <CheckIcon size={14} />
                          Concluir
                        </button>
                      )}
                      <RowActions
                        descricao={`a manutenção de ${m.tipoManutencaoNome} do veículo ${m.veiculoPlaca}`}
                        onEditar={podeCadastrar && pendente ? () => onEditar(m) : undefined}
                        onExcluir={podeExcluir ? () => onExcluir(m) : undefined}
                      />
                    </div>
                  </td>
                )}
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

/** Diálogo de conclusão — quilometragem, data, custo e observação da execução. */
function ConclusaoManutencaoFormulario({
  paraConcluir,
  form,
  onFormChange,
  onSubmit,
  onCancelar,
  pending,
  erros,
}: {
  paraConcluir: ManutencaoResponse
  form: FormularioConclusao
  onFormChange: (form: FormularioConclusao) => void
  onSubmit: (e: FormEvent) => void
  onCancelar: () => void
  pending: boolean
  erros: string[]
}) {
  return (
    <FormDialog
      titulo="Concluir manutenção"
      descricao={`${paraConcluir.tipoManutencaoNome} — ${paraConcluir.veiculoPlaca} (${paraConcluir.veiculoNome}). Se a quilometragem informada for maior que a atual do veículo, o odômetro dele é atualizado.`}
      textoConfirmar="Concluir"
      textoPendente="Concluindo…"
      pending={pending}
      erros={erros}
      onSubmit={onSubmit}
      onCancelar={onCancelar}
    >
      <SecaoCampos>
        <div className="field">
          <label htmlFor="quilometragemRealizada">Quilometragem realizada</label>
          <input
            id="quilometragemRealizada"
            className="input"
            type="number"
            min={1}
            max={2000000}
            required
            autoFocus
            value={form.quilometragemRealizada}
            onChange={(e) => onFormChange({ ...form, quilometragemRealizada: e.target.value })}
          />
        </div>
        <div className="field">
          <label htmlFor="dataRealizacao">Data da realização</label>
          <input
            id="dataRealizacao"
            className="input"
            type="date"
            required
            // A API recusa data futura (com margem de 1 dia por causa do fuso).
            max={hojeInputDate()}
            value={form.dataRealizacao}
            onChange={(e) => onFormChange({ ...form, dataRealizacao: e.target.value })}
          />
        </div>
        <div className="field">
          <label htmlFor="custo">Custo (opcional)</label>
          <input
            id="custo"
            className="input"
            type="number"
            min={0}
            step="0.01"
            placeholder="0,00"
            value={form.custo}
            onChange={(e) => onFormChange({ ...form, custo: e.target.value })}
          />
        </div>
        <div className="field campo-largo">
          <label htmlFor="observacaoConclusao">Observação (opcional)</label>
          <input
            id="observacaoConclusao"
            className="input"
            type="text"
            maxLength={500}
            placeholder="Ex.: trocado filtro junto"
            value={form.observacao}
            onChange={(e) => onFormChange({ ...form, observacao: e.target.value })}
          />
        </div>
      </SecaoCampos>
    </FormDialog>
  )
}

/**
 * Todo o estado, as consultas, as mutations e os handlers da tela — nada de JSX.
 * Extraído à parte (fix sugerido pelo React Doctor para `no-giant-component`:
 * "lift shared data fetching and effects into custom hooks") porque é isto,
 * mais as seções já extraídas acima, que fazia a função da página passar de
 * 300 linhas. `ManutencoesPage` abaixo só consome o retorno e monta a árvore.
 */
function useManutencoesController() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarManutencoes(user?.role)
  const podeGerenciarTipos = pode.editarTiposManutencao(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [filtroVeiculo, setFiltroVeiculo] = useState('')
  const [filtroStatus, setFiltroStatus] = useState('')
  const [periodo, setPeriodo] = useState<Periodo>('todos')

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<ManutencaoResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  // Guarda a última sugestão de km para não sobrescrever um valor digitado à mão.
  const [kmSugerido, setKmSugerido] = useState('')
  const [erros, setErros] = useState<string[]>([])

  const [paraConcluir, setParaConcluir] = useState<ManutencaoResponse | null>(null)
  const [formConclusao, setFormConclusao] = useState(CONCLUSAO_VAZIA)
  const [errosConclusao, setErrosConclusao] = useState<string[]>([])

  const [paraExcluir, setParaExcluir] = useState<ManutencaoResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  // O período vira `de`/`ate` aqui: a API não conhece "últimos 7 dias".
  const { de, ate } = intervaloDoPeriodo(periodo)

  const filtro: ManutencaoFiltro = {
    veiculoId: filtroVeiculo === '' ? undefined : Number(filtroVeiculo),
    status: filtroStatus === '' ? undefined : (filtroStatus as StatusManutencao),
    de,
    ate,
  }

  const temFiltro = filtroVeiculo !== '' || filtroStatus !== '' || periodo !== 'todos'

  const manutencoesQuery = useQuery({
    queryKey: ['manutencoes', filtro],
    queryFn: () => manutencoesApi.getAll(filtro),
  })
  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })
  // Só os ativos: um tipo aposentado no POST resulta em 422.
  const tiposQuery = useQuery({
    queryKey: ['tiposManutencao', 'ativos'],
    queryFn: () => tiposManutencaoApi.getAll(true),
  })

  const manutencoes = manutencoesQuery.data ?? []
  const paginacao = usePaginacao(manutencoes)
  const veiculos = veiculosQuery.data ?? []
  const tipos = tiposQuery.data ?? []

  // Memoizados sobre `*.data` (e não sobre os arrays com fallback `?? []`, que mudam
  // de identidade a cada render) — os mapas alimentam a sugestão de quilometragem.
  const veiculoPorId = useMemo(
    () => new Map((veiculosQuery.data ?? []).map((v) => [v.id, v])),
    [veiculosQuery.data],
  )
  const tipoPorId = useMemo(
    () => new Map((tiposQuery.data ?? []).map((t) => [t.id, t])),
    [tiposQuery.data],
  )

  /**
   * Atalho que cobre a maioria dos casos: km atual do veículo + intervalo do tipo.
   * Sem intervalo cadastrado não há o que sugerir.
   */
  function sugerirKm(veiculoId: string, tipoId: string): string {
    const veiculo = veiculoPorId.get(Number(veiculoId))
    const tipo = tipoPorId.get(Number(tipoId))
    if (!veiculo || !tipo?.intervaloKm) return ''
    return String(veiculo.quilometragem + tipo.intervaloKm)
  }

  /** Troca de veículo/tipo recalcula a sugestão, exceto se o operador já digitou um km próprio. */
  function aplicarSelecao(veiculoId: string, tipoId: string) {
    const nova = sugerirKm(veiculoId, tipoId)
    setForm((atual) => {
      const podeSubstituir = atual.quilometragemPrevista === '' || atual.quilometragemPrevista === kmSugerido
      return {
        ...atual,
        veiculoId,
        tipoManutencaoId: tipoId,
        quilometragemPrevista:
          nova !== '' && podeSubstituir ? nova : atual.quilometragemPrevista,
      }
    })
    if (nova !== '') setKmSugerido(nova)
  }

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: ManutencaoRequest }) =>
      id === null ? manutencoesApi.create(body) : manutencoesApi.update(id, body),
    onSuccess: () => {
      fecharForm()
      queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
      // Editar pode trocar o veículo de uma manutenção já concluída, e com ele o veículo
      // a que o custo é atribuído.
      queryClient.invalidateQueries({ queryKey: ['custos'] })
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível agendar a manutenção.',
        ),
      ),
  })

  const concluirMutation = useMutation({
    mutationFn: ({ id, body }: { id: number; body: ConcluirManutencaoRequest }) =>
      manutencoesApi.concluir(id, body),
    onSuccess: () => {
      fecharConclusao()
      queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
      // Concluir pode ter avançado o odômetro do veículo, o que muda `atrasada` e
      // `kmRestantes` das outras manutenções dele — e a quilometragem na tela de veículos.
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
      // É aqui que o custo da manutenção entra no sistema.
      queryClient.invalidateQueries({ queryKey: ['custos'] })
    },
    onError: (error) => setErrosConclusao(mensagensDeErro(error, 'Não foi possível concluir a manutenção.')),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => manutencoesApi.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
      // Excluir uma manutenção concluída tira o custo dela do total.
      queryClient.invalidateQueries({ queryKey: ['custos'] })
    },
    onError: (error) => setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir a manutenção.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    salvarMutation.mutate({
      id: editando?.id ?? null,
      body: {
        veiculoId: Number(form.veiculoId),
        tipoManutencaoId: Number(form.tipoManutencaoId),
        quilometragemPrevista: Number(form.quilometragemPrevista),
        dataPrevista: form.dataPrevista || null,
        observacao: form.observacao.trim() || null,
      },
    })
  }

  function handleConcluir(e: FormEvent) {
    e.preventDefault()
    if (!paraConcluir) return
    concluirMutation.mutate({
      id: paraConcluir.id,
      body: {
        quilometragemRealizada: Number(formConclusao.quilometragemRealizada),
        dataRealizacao: formConclusao.dataRealizacao,
        custo: formConclusao.custo === '' ? null : Number(formConclusao.custo),
        observacao: formConclusao.observacao.trim() || null,
      },
    })
  }

  function abrirCadastro() {
    setEditando(null)
    setForm(FORM_VAZIO)
    setKmSugerido('')
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(m: ManutencaoResponse) {
    setEditando(m)
    setForm({
      veiculoId: String(m.veiculoId),
      tipoManutencaoId: String(m.tipoManutencaoId),
      quilometragemPrevista: String(m.quilometragemPrevista),
      dataPrevista: paraInputDate(m.dataPrevista),
      observacao: m.observacao ?? '',
    })
    setKmSugerido('')
    setErros([])
    setAberto(true)
  }

  function fecharForm() {
    setAberto(false)
    setEditando(null)
    setForm(FORM_VAZIO)
    setKmSugerido('')
    setErros([])
  }

  function abrirConclusao(m: ManutencaoResponse) {
    setParaConcluir(m)
    setFormConclusao({
      // O km atual do veículo é o palpite mais próximo do real.
      quilometragemRealizada: String(m.quilometragemAtualVeiculo),
      dataRealizacao: hojeInputDate(),
      custo: '',
      observacao: m.observacao ?? '',
    })
    setErrosConclusao([])
  }

  function fecharConclusao() {
    setParaConcluir(null)
    setFormConclusao(CONCLUSAO_VAZIA)
    setErrosConclusao([])
  }

  const semVeiculos = veiculosQuery.isSuccess && veiculos.length === 0
  const semTipos = tiposQuery.isSuccess && tipos.length === 0
  const semCadastrosBase = semVeiculos || semTipos

  const mostrarAcoes = podeCadastrar || podeExcluir
  // A API zera o custo para o motorista; exibir a coluna renderizaria uma fileira de
  // traços. Esconder é mais honesto do que mostrar vazio.
  const mostrarCusto = !pode.verMinhasRotas(user?.role)
  const colunas = (mostrarAcoes ? 7 : 6) - (mostrarCusto ? 0 : 1)

  return {
    podeCadastrar,
    podeGerenciarTipos,
    podeExcluir,
    filtroVeiculo,
    setFiltroVeiculo,
    filtroStatus,
    setFiltroStatus,
    periodo,
    setPeriodo,
    aberto,
    editando,
    form,
    setForm,
    erros,
    paraConcluir,
    formConclusao,
    setFormConclusao,
    errosConclusao,
    paraExcluir,
    errosExclusao,
    setErrosExclusao,
    setParaExcluir,
    temFiltro,
    manutencoesQuery,
    manutencoes,
    paginacao,
    veiculos,
    tipos,
    aplicarSelecao,
    salvarMutation,
    concluirMutation,
    excluirMutation,
    handleSubmit,
    handleConcluir,
    abrirCadastro,
    abrirEdicao,
    fecharForm,
    abrirConclusao,
    fecharConclusao,
    semTipos,
    semCadastrosBase,
    mostrarAcoes,
    mostrarCusto,
    colunas,
  }
}

export function ManutencoesPage() {
  const c = useManutencoesController()

  return (
    <AppLayout>
      <PageHeader
        titulo="Manutenções"
        subtitulo="Manutenção preventiva da frota — pendentes primeiro, vencendo antes no topo."
        acoes={
          c.podeCadastrar && (
            <button
              type="button"
              className="btn btn-primary"
              onClick={c.abrirCadastro}
              disabled={c.semCadastrosBase}
              title={
                c.semCadastrosBase
                  ? 'É preciso ter ao menos um veículo e um tipo de manutenção ativo.'
                  : undefined
              }
            >
              Nova manutenção
            </button>
          )
        }
      />

      {/*
        Empresas provisionadas antes da manutenção preventiva não receberam o catálogo
        padrão — sem tipo cadastrado o seletor viria vazio, então o aviso é explícito.
      */}
      {c.semTipos && (
        <div
          className="mb-6 p-4 text-[13px]"
          style={{ border: '1px solid var(--color-accent-300)', background: 'var(--color-accent-100)' }}
        >
          <strong>Nenhum tipo de manutenção ativo.</strong> O catálogo de tipos é o que alimenta o
          seletor de agendamento.{' '}
          {c.podeGerenciarTipos ? (
            <Link to="/tipos-manutencao">Cadastre o primeiro tipo</Link>
          ) : (
            'Peça a um administrador ou supervisor para cadastrar o primeiro.'
          )}
        </div>
      )}

      {c.aberto && c.podeCadastrar && (
        <ManutencaoFormulario
          editando={c.editando}
          form={c.form}
          onFormChange={c.setForm}
          onAplicarSelecao={c.aplicarSelecao}
          onSubmit={c.handleSubmit}
          onCancelar={c.fecharForm}
          pending={c.salvarMutation.isPending}
          erros={c.erros}
          veiculos={c.veiculos}
          tipos={c.tipos}
        />
      )}

      <FiltrosManutencao
        veiculos={c.veiculos}
        filtroVeiculo={c.filtroVeiculo}
        filtroStatus={c.filtroStatus}
        periodo={c.periodo}
        temFiltro={c.temFiltro}
        onFiltroVeiculoChange={c.setFiltroVeiculo}
        onFiltroStatusChange={c.setFiltroStatus}
        onPeriodoChange={c.setPeriodo}
        onLimpar={() => {
          c.setFiltroVeiculo('')
          c.setFiltroStatus('')
          c.setPeriodo('todos')
        }}
      />

      <TabelaManutencoes
        manutencoes={c.paginacao.itensDaPagina}
        colunas={c.colunas}
        mostrarCusto={c.mostrarCusto}
        mostrarAcoes={c.mostrarAcoes}
        podeCadastrar={c.podeCadastrar}
        podeExcluir={c.podeExcluir}
        temFiltro={c.temFiltro}
        pending={c.manutencoesQuery.isPending}
        error={c.manutencoesQuery.error}
        isSuccess={c.manutencoesQuery.isSuccess}
        onConcluir={c.abrirConclusao}
        onEditar={c.abrirEdicao}
        onExcluir={(m) => {
          c.setErrosExclusao([])
          c.setParaExcluir(m)
        }}
      />

      <Paginacao {...c.paginacao} pending={c.manutencoesQuery.isFetching} />

      {c.paraConcluir && (
        <ConclusaoManutencaoFormulario
          paraConcluir={c.paraConcluir}
          form={c.formConclusao}
          onFormChange={c.setFormConclusao}
          onSubmit={c.handleConcluir}
          onCancelar={c.fecharConclusao}
          pending={c.concluirMutation.isPending}
          erros={c.errosConclusao}
        />
      )}

      {c.paraExcluir && (
        <ConfirmDialog
          titulo="Excluir manutenção"
          mensagem={`A manutenção de ${c.paraExcluir.tipoManutencaoNome} do veículo ${c.paraExcluir.veiculoPlaca} será removida. Esta ação não pode ser desfeita.`}
          pending={c.excluirMutation.isPending}
          erros={c.errosExclusao}
          // Non-null seguro: só renderiza dentro do `c.paraExcluir &&` acima — TS não
          // propaga o narrowing de uma propriedade de objeto para dentro do closure.
          onConfirmar={() => c.excluirMutation.mutate(c.paraExcluir!.id)}
          onCancelar={() => {
            c.setParaExcluir(null)
            c.setErrosExclusao([])
          }}
        />
      )}
    </AppLayout>
  )
}
