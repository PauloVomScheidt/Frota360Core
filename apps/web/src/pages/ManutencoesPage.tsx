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
} from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, FormDialog, InlineForm, RowActions, TableStates } from '../components/Table'
import { CheckIcon } from '../components/icons'
import { formatDate, formatKm, formatMoeda, hojeInputDate, paraInputDate } from '../lib/format'

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

/**
 * `atrasada` tem precedência sobre `status`: é o campo que a API recalcula a cada
 * leitura comparando o km previsto com a quilometragem atual do veículo (§7.3.3).
 * `Cancelada` não é produzida por nenhum endpoint hoje, mas o enum a prevê.
 */
function badgeDaManutencao(m: ManutencaoResponse): { rotulo: string; classe: string } {
  if (m.atrasada) return { rotulo: 'Atrasada', classe: 'tag tag-danger' }
  if (m.status === 'Pendente') return { rotulo: 'Pendente', classe: 'tag tag-accent' }
  if (m.status === 'Realizada') return { rotulo: 'Concluída', classe: 'tag tag-neutral' }
  return { rotulo: 'Cancelada', classe: 'tag tag-neutral' }
}

/** `kmRestantes` vem negativo quando o veículo já passou do ponto, e null fora de "Pendente". */
function textoKmRestantes(km: number | null | undefined): string | null {
  if (km == null) return null
  if (km < 0) return `${Math.abs(km).toLocaleString('pt-BR')} km em atraso`
  return `faltam ${km.toLocaleString('pt-BR')} km`
}

export function ManutencoesPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarManutencoes(user?.role)
  const podeGerenciarTipos = pode.editarTiposManutencao(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [filtroVeiculo, setFiltroVeiculo] = useState('')
  const [filtroStatus, setFiltroStatus] = useState('')

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

  const filtro: ManutencaoFiltro = {
    veiculoId: filtroVeiculo === '' ? undefined : Number(filtroVeiculo),
    status: filtroStatus === '' ? undefined : (filtroStatus as StatusManutencao),
  }

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
    // O formulário abre acima da tabela — a linha editada pode estar fora da tela.
    window.scrollTo({ top: 0, behavior: 'smooth' })
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
  const colunas = mostrarAcoes ? 7 : 6

  return (
    <AppLayout>
      <PageHeader
        titulo="Manutenções"
        subtitulo="Manutenção preventiva da frota — pendentes primeiro, vencendo antes no topo."
        acoes={
          podeCadastrar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
              disabled={semCadastrosBase && !aberto}
              title={
                semCadastrosBase
                  ? 'É preciso ter ao menos um veículo e um tipo de manutenção ativo.'
                  : undefined
              }
            >
              {aberto ? 'Cancelar' : 'Nova manutenção'}
            </button>
          )
        }
      />

      {/*
        Empresas provisionadas antes da manutenção preventiva não receberam o catálogo
        padrão — sem tipo cadastrado o seletor viria vazio, então o aviso é explícito.
      */}
      {semTipos && (
        <div
          className="mb-6 p-4 text-[13px]"
          style={{ border: '1px solid var(--color-accent-300)', background: 'var(--color-accent-100)' }}
        >
          <strong>Nenhum tipo de manutenção ativo.</strong> O catálogo de tipos é o que alimenta o
          seletor de agendamento.{' '}
          {podeGerenciarTipos ? (
            <Link to="/tipos-manutencao">Cadastre o primeiro tipo</Link>
          ) : (
            'Peça a um administrador ou supervisor para cadastrar o primeiro.'
          )}
        </div>
      )}

      {aberto && podeCadastrar && (
        <InlineForm onSubmit={handleSubmit}>
          {editando && (
            <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
              Editando a manutenção de{' '}
              <strong style={{ color: 'var(--color-text)' }}>{editando.tipoManutencaoNome}</strong> do
              veículo {editando.veiculoPlaca}.
            </p>
          )}
          <div className="field w-[230px]">
            <label htmlFor="veiculoId">Veículo</label>
            <select
              id="veiculoId"
              className="input"
              required
              style={{ borderRadius: 0 }}
              value={form.veiculoId}
              onChange={(e) => aplicarSelecao(e.target.value, form.tipoManutencaoId)}
            >
              <option value="">Selecione…</option>
              {veiculos.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.placa} — {v.nomeVeiculo} ({formatKm(v.quilometragem)})
                </option>
              ))}
            </select>
          </div>
          <div className="field w-[230px]">
            <label htmlFor="tipoManutencaoId">Tipo</label>
            <select
              id="tipoManutencaoId"
              className="input"
              required
              style={{ borderRadius: 0 }}
              value={form.tipoManutencaoId}
              onChange={(e) => aplicarSelecao(form.veiculoId, e.target.value)}
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
          <div className="field w-[190px]">
            <label htmlFor="quilometragemPrevista">Quilometragem prevista</label>
            <input
              id="quilometragemPrevista"
              className="input"
              type="number"
              min={1}
              max={2000000}
              required
              placeholder="0"
              style={{ borderRadius: 0 }}
              value={form.quilometragemPrevista}
              onChange={(e) => setForm({ ...form, quilometragemPrevista: e.target.value })}
            />
          </div>
          <div className="field w-[170px]">
            <label htmlFor="dataPrevista">Prazo (opcional)</label>
            <input
              id="dataPrevista"
              className="input"
              type="date"
              // No agendamento novo a API recusa data no passado; na edição, permite replanejar.
              min={editando ? undefined : hojeInputDate()}
              style={{ borderRadius: 0 }}
              value={form.dataPrevista}
              onChange={(e) => setForm({ ...form, dataPrevista: e.target.value })}
            />
          </div>
          <div className="field min-w-[220px] flex-1">
            <label htmlFor="observacao">Observação (opcional)</label>
            <input
              id="observacao"
              className="input"
              type="text"
              maxLength={500}
              placeholder="Ex.: levar filtro sobressalente"
              style={{ borderRadius: 0 }}
              value={form.observacao}
              onChange={(e) => setForm({ ...form, observacao: e.target.value })}
            />
          </div>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ borderRadius: 0, padding: '10px 20px' }}
            disabled={salvarMutation.isPending}
          >
            {salvarMutation.isPending ? 'Salvando…' : editando ? 'Salvar alterações' : 'Agendar'}
          </button>
          <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
            A manutenção vence no que vier primeiro: a quilometragem prevista ou o prazo. Quando o tipo
            tem intervalo cadastrado, a quilometragem já vem sugerida (km atual do veículo + intervalo).
          </p>
          <div className="w-full">
            <ErrorList mensagens={erros} />
          </div>
        </InlineForm>
      )}

      <div className="mb-5 flex flex-wrap items-end gap-4">
        <div className="field w-[230px]">
          <label htmlFor="filtroVeiculo">Filtrar por veículo</label>
          <select
            id="filtroVeiculo"
            className="input"
            style={{ borderRadius: 0 }}
            value={filtroVeiculo}
            onChange={(e) => setFiltroVeiculo(e.target.value)}
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
            onChange={(e) => setFiltroStatus(e.target.value)}
          >
            <option value="">Todas</option>
            <option value="Pendente">Pendentes</option>
            <option value="Realizada">Concluídas</option>
          </select>
        </div>
        {(filtroVeiculo !== '' || filtroStatus !== '') && (
          <button
            type="button"
            className="btn btn-secondary"
            style={{ borderRadius: 0, padding: '10px 18px' }}
            onClick={() => {
              setFiltroVeiculo('')
              setFiltroStatus('')
            }}
          >
            Limpar filtros
          </button>
        )}
      </div>

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Veículo</th>
              <th>Tipo</th>
              <th>Quilometragem prevista</th>
              <th>Situação</th>
              <th>Andamento</th>
              <th>Custo</th>
              {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={colunas}
              pending={manutencoesQuery.isPending}
              error={manutencoesQuery.error}
              empty={manutencoesQuery.isSuccess && manutencoes.length === 0}
              textoCarregando="Carregando manutenções…"
              textoErro="Não foi possível carregar as manutenções."
              textoVazio={
                filtroVeiculo !== '' || filtroStatus !== ''
                  ? 'Nenhuma manutenção encontrada com esses filtros.'
                  : 'Nenhuma manutenção agendada ainda.'
              }
            />
            {manutencoes.map((m) => {
              const badge = badgeDaManutencao(m)
              const pendente = m.status === 'Pendente'
              const restante = textoKmRestantes(m.kmRestantes)
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
                      <span style={m.atrasada ? { color: 'var(--color-danger)' } : undefined}>
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
                  <td>{formatMoeda(m.custo)}</td>
                  {mostrarAcoes && (
                    <td>
                      <div className="flex items-center justify-end gap-1">
                        {/* Editar e concluir só valem em linha pendente — o resto é histórico. */}
                        {podeCadastrar && pendente && (
                          <button
                            type="button"
                            className="btn btn-secondary"
                            style={{ borderRadius: 0, padding: '6px 12px', fontSize: 12 }}
                            onClick={() => abrirConclusao(m)}
                          >
                            <CheckIcon size={14} />
                            Concluir
                          </button>
                        )}
                        <RowActions
                          descricao={`a manutenção de ${m.tipoManutencaoNome} do veículo ${m.veiculoPlaca}`}
                          onEditar={podeCadastrar && pendente ? () => abrirEdicao(m) : undefined}
                          onExcluir={
                            podeExcluir
                              ? () => {
                                  setErrosExclusao([])
                                  setParaExcluir(m)
                                }
                              : undefined
                          }
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

      {paraConcluir && (
        <FormDialog
          titulo="Concluir manutenção"
          descricao={`${paraConcluir.tipoManutencaoNome} — ${paraConcluir.veiculoPlaca} (${paraConcluir.veiculoNome}). Se a quilometragem informada for maior que a atual do veículo, o odômetro dele é atualizado.`}
          textoConfirmar="Concluir"
          textoPendente="Concluindo…"
          pending={concluirMutation.isPending}
          erros={errosConclusao}
          onSubmit={handleConcluir}
          onCancelar={fecharConclusao}
        >
          <div className="field w-[190px]">
            <label htmlFor="quilometragemRealizada">Quilometragem realizada</label>
            <input
              id="quilometragemRealizada"
              className="input"
              type="number"
              min={1}
              max={2000000}
              required
              autoFocus
              style={{ borderRadius: 0 }}
              value={formConclusao.quilometragemRealizada}
              onChange={(e) =>
                setFormConclusao({ ...formConclusao, quilometragemRealizada: e.target.value })
              }
            />
          </div>
          <div className="field w-[170px]">
            <label htmlFor="dataRealizacao">Data da realização</label>
            <input
              id="dataRealizacao"
              className="input"
              type="date"
              required
              // A API recusa data futura (com margem de 1 dia por causa do fuso).
              max={hojeInputDate()}
              style={{ borderRadius: 0 }}
              value={formConclusao.dataRealizacao}
              onChange={(e) => setFormConclusao({ ...formConclusao, dataRealizacao: e.target.value })}
            />
          </div>
          <div className="field w-[150px]">
            <label htmlFor="custo">Custo (opcional)</label>
            <input
              id="custo"
              className="input"
              type="number"
              min={0}
              step="0.01"
              placeholder="0,00"
              style={{ borderRadius: 0 }}
              value={formConclusao.custo}
              onChange={(e) => setFormConclusao({ ...formConclusao, custo: e.target.value })}
            />
          </div>
          <div className="field w-full">
            <label htmlFor="observacaoConclusao">Observação (opcional)</label>
            <input
              id="observacaoConclusao"
              className="input"
              type="text"
              maxLength={500}
              placeholder="Ex.: trocado filtro junto"
              style={{ borderRadius: 0 }}
              value={formConclusao.observacao}
              onChange={(e) => setFormConclusao({ ...formConclusao, observacao: e.target.value })}
            />
          </div>
        </FormDialog>
      )}

      {paraExcluir && (
        <ConfirmDialog
          titulo="Excluir manutenção"
          mensagem={`A manutenção de ${paraExcluir.tipoManutencaoNome} do veículo ${paraExcluir.veiculoPlaca} será removida. Esta ação não pode ser desfeita.`}
          pending={excluirMutation.isPending}
          erros={errosExclusao}
          onConfirmar={() => excluirMutation.mutate(paraExcluir.id)}
          onCancelar={() => {
            setParaExcluir(null)
            setErrosExclusao([])
          }}
        />
      )}
    </AppLayout>
  )
}
