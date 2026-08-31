import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { abastecimentosApi } from '../api/abastecimentos'
import { motoristasApi } from '../api/motoristas'
import { rotasApi } from '../api/rotas'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import type {
  AbastecimentoFiltro,
  AbastecimentoResponse,
  MotoristaResponse,
  RotaResponse,
  VeiculoResponse,
} from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, FiltroPeriodo, InlineForm, RowActions, TableStates } from '../components/Table'
import { formatDate, formatMoeda, hojeInputDate, paraInputDate } from '../lib/format'
import { intervaloDoPeriodo, type Periodo } from '../lib/periodo'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = {
  veiculoId: '',
  motoristaId: '',
  valor: '',
  dataAbastecimento: '',
  observacao: '',
}

type FormularioAbastecimento = typeof FORM_VAZIO

/**
 * Painel de cadastro/edição — extraído à parte por ser, sozinho, o maior bloco de JSX
 * da tela (as três variações de campo motorista/veículo conforme papel e estado de
 * edição). Nenhuma regra muda de lugar: cada `onFormChange` chama o mesmo `setForm`
 * que já existia aqui dentro.
 */
function AbastecimentoFormulario({
  editando,
  form,
  onFormChange,
  onSubmit,
  pending,
  erros,
  veiculosDisponiveis,
  veiculos,
  motoristas,
  motorista,
  nomeUsuario,
  rotaAtiva,
}: {
  editando: AbastecimentoResponse | null
  form: FormularioAbastecimento
  onFormChange: (form: FormularioAbastecimento) => void
  onSubmit: (e: FormEvent) => void
  pending: boolean
  erros: string[]
  veiculosDisponiveis: VeiculoResponse[]
  veiculos: VeiculoResponse[]
  motoristas: MotoristaResponse[]
  motorista: boolean
  nomeUsuario: string
  rotaAtiva: RotaResponse | null
}) {
  return (
    <InlineForm onSubmit={onSubmit}>
      {editando && (
        <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
          Corrigindo o abastecimento do veículo{' '}
          <strong style={{ color: 'var(--color-text)' }}>{editando.veiculoPlaca}</strong> de{' '}
          {formatDate(editando.dataAbastecimento)}. Veículo e motorista não podem ser trocados
          — para isso, exclua e lance de novo.
        </p>
      )}

      {/* Trocar o veículo reatribuiria o gasto: só no cadastro. */}
      {!editando && (
        <div className="field w-[230px]">
          <label htmlFor="veiculoId">Veículo</label>
          <select
            id="veiculoId"
            className="input"
            required
            style={{ borderRadius: 0 }}
            value={form.veiculoId}
            onChange={(e) => onFormChange({ ...form, veiculoId: e.target.value })}
          >
            <option value="">Selecione…</option>
            {veiculosDisponiveis.map((v) => (
              <option key={v.id} value={v.id}>
                {v.placa} — {v.nomeVeiculo}
              </option>
            ))}
          </select>
        </div>
      )}

      {/* O motorista lança sempre em si mesmo — o campo existe para ele ver de quem é
          o gasto, não para escolher. Quem escolhe é a gestão. */}
      {!editando &&
        (motorista ? (
          <div className="field w-[200px]">
            <label htmlFor="motoristaId">Motorista</label>
            <input
              id="motoristaId"
              className="input"
              disabled
              style={{ borderRadius: 0 }}
              value={nomeUsuario}
            />
          </div>
        ) : (
          <div className="field w-[200px]">
            <label htmlFor="motoristaId">Motorista</label>
            <select
              id="motoristaId"
              className="input"
              required
              style={{ borderRadius: 0 }}
              value={form.motoristaId}
              onChange={(e) => onFormChange({ ...form, motoristaId: e.target.value })}
            >
              <option value="">Selecione…</option>
              {motoristas.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.nome}
                </option>
              ))}
            </select>
          </div>
        ))}

      <div className="field w-[150px]">
        <label htmlFor="valor">Valor (R$)</label>
        <input
          id="valor"
          type="number"
          className="input"
          required
          min={0.01}
          step={0.01}
          placeholder="0,00"
          style={{ borderRadius: 0 }}
          value={form.valor}
          onChange={(e) => onFormChange({ ...form, valor: e.target.value })}
        />
      </div>

      <div className="field w-[170px]">
        <label htmlFor="dataAbastecimento">Data</label>
        <input
          id="dataAbastecimento"
          type="date"
          className="input"
          required
          max={hojeInputDate()}
          style={{ borderRadius: 0 }}
          value={form.dataAbastecimento}
          onChange={(e) => onFormChange({ ...form, dataAbastecimento: e.target.value })}
        />
      </div>

      <div className="field w-[260px]">
        <label htmlFor="observacao">Observação</label>
        <input
          id="observacao"
          className="input"
          maxLength={500}
          style={{ borderRadius: 0 }}
          value={form.observacao}
          onChange={(e) => onFormChange({ ...form, observacao: e.target.value })}
        />
      </div>

      <button
        type="submit"
        className="btn btn-primary"
        style={{ borderRadius: 0, padding: '10px 20px' }}
        disabled={pending}
      >
        {pending ? 'Salvando…' : editando ? 'Salvar correção' : 'Lançar'}
      </button>

      {!editando && motorista && rotaAtiva && (
        <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
          Você está em rota com{' '}
          <strong style={{ color: 'var(--color-text)' }}>
            {veiculos.find((v) => v.id === rotaAtiva.codigoVeiculo)?.placa ?? 'o veículo da rota'}
          </strong>{' '}
          ({rotaAtiva.origem} → {rotaAtiva.destino}) — o lançamento vai para esse veículo e fica
          vinculado à viagem.
        </p>
      )}

      <div className="w-full">
        <ErrorList mensagens={erros} />
      </div>
    </InlineForm>
  )
}

/** Barra de filtros — veículo, motorista (fora do motorista) e período. */
function FiltrosAbastecimento({
  veiculos,
  motoristas,
  motorista,
  filtroVeiculo,
  filtroMotorista,
  periodo,
  temFiltro,
  onFiltroVeiculoChange,
  onFiltroMotoristaChange,
  onPeriodoChange,
  onLimpar,
}: {
  veiculos: VeiculoResponse[]
  motoristas: MotoristaResponse[]
  motorista: boolean
  filtroVeiculo: string
  filtroMotorista: string
  periodo: Periodo
  temFiltro: boolean
  onFiltroVeiculoChange: (v: string) => void
  onFiltroMotoristaChange: (v: string) => void
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
      {/* A lista do motorista já vem recortada: filtrar por pessoa não faria sentido lá. */}
      {!motorista && (
        <div className="field w-[200px]">
          <label htmlFor="filtroMotorista">Filtrar por motorista</label>
          <select
            id="filtroMotorista"
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
      )}
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
    </div>
  )
}

/** Tabela + rodapé de totais — a listagem propriamente dita. */
function TabelaAbastecimentos({
  abastecimentos,
  motorista,
  mostrarAcoes,
  podeLancar,
  podeExcluir,
  temFiltro,
  pending,
  error,
  isSuccess,
  total,
  onEditar,
  onExcluir,
}: {
  abastecimentos: AbastecimentoResponse[]
  motorista: boolean
  mostrarAcoes: boolean
  podeLancar: boolean
  podeExcluir: boolean
  temFiltro: boolean
  pending: boolean
  error: unknown
  isSuccess: boolean
  total: number
  onEditar: (a: AbastecimentoResponse) => void
  onExcluir: (a: AbastecimentoResponse) => void
}) {
  return (
    <>
      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Veículo</th>
              <th>Motorista</th>
              {!motorista && <th>Quem lançou</th>}
              <th>Valor</th>
              <th>Observação</th>
              {mostrarAcoes && <th style={{ width: 90 }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={(motorista ? 5 : 6) + (mostrarAcoes ? 1 : 0)}
              pending={pending}
              error={error}
              empty={isSuccess && abastecimentos.length === 0}
              textoCarregando="Carregando abastecimentos…"
              textoErro="Não foi possível carregar os abastecimentos."
              textoVazio={
                temFiltro
                  ? 'Nenhum abastecimento encontrado com esses filtros.'
                  : 'Nenhum abastecimento lançado ainda.'
              }
            />
            {abastecimentos.map((a) => (
              <tr key={a.id}>
                <td style={{ whiteSpace: 'nowrap' }}>{formatDate(a.dataAbastecimento)}</td>
                <td>
                  <div>{a.veiculoPlaca}</div>
                  <div className="text-[12px]" style={{ color: mutedText }}>
                    {a.rotaDescricao ?? a.veiculoNome}
                  </div>
                </td>
                <td>{a.motoristaNome}</td>
                {!motorista && <td>{a.usuarioNome}</td>}
                <td style={{ whiteSpace: 'nowrap' }}>{formatMoeda(a.valor)}</td>
                <td style={{ color: a.observacao ? undefined : mutedText }}>
                  {a.observacao ?? '—'}
                </td>
                {mostrarAcoes && (
                  <td>
                    <RowActions
                      // A lista do motorista já vem recortada pelo servidor: toda linha
                      // que ele enxerga é dele, então não há o que esconder aqui.
                      onEditar={podeLancar ? () => onEditar(a) : undefined}
                      onExcluir={podeExcluir ? () => onExcluir(a) : undefined}
                      descricao={`o abastecimento do veículo ${a.veiculoPlaca} de ${formatDate(a.dataAbastecimento)}`}
                    />
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {abastecimentos.length > 0 && (
        <div
          className="mt-4 flex flex-wrap items-center gap-6 py-3 text-[13px]"
          style={{ borderTop: '1px solid var(--color-divider)', color: mutedText }}
        >
          <span>
            <strong style={{ color: 'var(--color-text)' }}>{abastecimentos.length}</strong>{' '}
            {abastecimentos.length === 1 ? 'lançamento' : 'lançamentos'}
          </span>
          <span>
            Total: <strong style={{ color: 'var(--color-text)' }}>{formatMoeda(total)}</strong>
          </span>
        </div>
      )}
    </>
  )
}

export function AbastecimentosPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeLancar = pode.lancarAbastecimento(user?.role)
  const podeExcluir = pode.excluir(user?.role)
  const motorista = pode.verMinhasRotas(user?.role)

  const [filtroVeiculo, setFiltroVeiculo] = useState('')
  const [filtroMotorista, setFiltroMotorista] = useState('')
  const [periodo, setPeriodo] = useState<Periodo>('todos')

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<AbastecimentoResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])

  const [paraExcluir, setParaExcluir] = useState<AbastecimentoResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  // O período vira `de`/`ate` aqui: a API não conhece "últimos 7 dias".
  const { de, ate } = intervaloDoPeriodo(periodo)

  const filtro: AbastecimentoFiltro = {
    veiculoId: filtroVeiculo === '' ? undefined : Number(filtroVeiculo),
    motoristaId: filtroMotorista === '' ? undefined : Number(filtroMotorista),
    de,
    ate,
  }

  const temFiltro = filtroVeiculo !== '' || filtroMotorista !== '' || periodo !== 'todos'

  const abastecimentosQuery = useQuery({
    queryKey: ['abastecimentos', filtro],
    queryFn: () => abastecimentosApi.getAll(filtro),
  })
  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })

  // `GET /motorista` é restrito à gestão: pedir a lista como motorista devolveria 403.
  const motoristasQuery = useQuery({
    queryKey: ['motoristas'],
    queryFn: motoristasApi.getAll,
    enabled: !motorista,
  })

  // O motorista lança no carro da rota aberta, quando tem uma. É a mesma derivação de
  // MinhasRotasPage — "ativa" não é um endpoint, é `ativo` na lista dele.
  const minhasRotasQuery = useQuery({
    queryKey: ['rotas', 'minhas'],
    queryFn: rotasApi.getMinhas,
    enabled: motorista,
  })

  const abastecimentos = abastecimentosQuery.data ?? []
  const veiculos = veiculosQuery.data ?? []
  const motoristas = motoristasQuery.data ?? []
  const rotaAtiva = (minhasRotasQuery.data ?? []).find((r) => r.ativo) ?? null

  // Com rota aberta o veículo é um só: mostrar a frota inteira seria oferecer o que o
  // servidor vai recusar com 422.
  // Memoizado sobre `*.data`, não sobre o array com fallback `?? []`, que muda de
  // identidade a cada render (mesmo cuidado do total, abaixo).
  const veiculosDisponiveis = useMemo(() => {
    const lista = veiculosQuery.data ?? []
    if (!rotaAtiva) return lista
    return lista.filter((v) => v.id === rotaAtiva.codigoVeiculo)
  }, [veiculosQuery.data, rotaAtiva])

  /** Sem odômetro em jogo, o lançamento não toca em veículo nem em manutenção. */
  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ['abastecimentos'] })
  }

  const salvarMutation = useMutation({
    mutationFn: () => {
      const corpo = {
        valor: Number(form.valor),
        dataAbastecimento: form.dataAbastecimento,
        observacao: form.observacao === '' ? null : form.observacao,
      }

      if (editando) return abastecimentosApi.update(editando.id, corpo)

      return abastecimentosApi.create({
        ...corpo,
        veiculoId: Number(form.veiculoId),
        // O motorista não manda o campo: a API o resolve pelo token.
        motoristaId: motorista ? undefined : Number(form.motoristaId),
      })
    },
    onSuccess: () => {
      setErros([])
      fecharForm()
      invalidar()
    },
    onError: (error) =>
      setErros(mensagensDeErro(error, 'Não foi possível salvar o abastecimento.')),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => abastecimentosApi.remove(id),
    onSuccess: () => {
      setParaExcluir(null)
      setErrosExclusao([])
      invalidar()
    },
    onError: (error) =>
      setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir o abastecimento.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    salvarMutation.mutate()
  }

  function abrirCadastro() {
    setEditando(null)
    setForm({
      ...FORM_VAZIO,
      dataAbastecimento: hojeInputDate(),
      // Em rota aberta o carro já está decidido — um clique a menos no posto.
      veiculoId: rotaAtiva ? String(rotaAtiva.codigoVeiculo) : '',
    })
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(a: AbastecimentoResponse) {
    setEditando(a)
    setForm({
      veiculoId: String(a.veiculoId),
      motoristaId: String(a.motoristaId),
      valor: String(a.valor),
      dataAbastecimento: paraInputDate(a.dataAbastecimento),
      observacao: a.observacao ?? '',
    })
    setErros([])
    setAberto(true)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function fecharForm() {
    setAberto(false)
    setEditando(null)
    setForm(FORM_VAZIO)
  }

  const semVeiculos = veiculosQuery.isSuccess && veiculos.length === 0
  const semMotoristas = !motorista && motoristasQuery.isSuccess && motoristas.length === 0
  const naoPodeAbrir = semVeiculos || semMotoristas
  const mostrarAcoes = podeLancar || podeExcluir

  // Total do que está na tela — o recorte é o do filtro, não da frota inteira.
  // Memoizado sobre `*.data` e não sobre o array com fallback `?? []`, que muda de
  // identidade a cada render (mesmo cuidado de ManutencoesPage).
  const total = useMemo(
    () => (abastecimentosQuery.data ?? []).reduce((s, a) => s + a.valor, 0),
    [abastecimentosQuery.data],
  )

  return (
    <AppLayout>
      <PageHeader
        titulo="Abastecimentos"
        subtitulo={
          motorista
            ? 'Seus abastecimentos. Registre o gasto em poucos campos — veículo, valor e data.'
            : 'Combustível da frota. Cada lançamento fica atribuído a um motorista, para o gasto por pessoa, veículo e período.'
        }
        acoes={
          podeLancar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
              disabled={naoPodeAbrir && !aberto}
              title={
                semVeiculos
                  ? 'É preciso ter ao menos um veículo cadastrado.'
                  : semMotoristas
                    ? 'É preciso ter ao menos um motorista cadastrado.'
                    : undefined
              }
            >
              {aberto ? 'Cancelar' : 'Novo abastecimento'}
            </button>
          )
        }
      />

      {aberto && podeLancar && (
        <AbastecimentoFormulario
          editando={editando}
          form={form}
          onFormChange={setForm}
          onSubmit={handleSubmit}
          pending={salvarMutation.isPending}
          erros={erros}
          veiculosDisponiveis={veiculosDisponiveis}
          veiculos={veiculos}
          motoristas={motoristas}
          motorista={motorista}
          nomeUsuario={user?.nome ?? ''}
          rotaAtiva={rotaAtiva}
        />
      )}

      <FiltrosAbastecimento
        veiculos={veiculos}
        motoristas={motoristas}
        motorista={motorista}
        filtroVeiculo={filtroVeiculo}
        filtroMotorista={filtroMotorista}
        periodo={periodo}
        temFiltro={temFiltro}
        onFiltroVeiculoChange={setFiltroVeiculo}
        onFiltroMotoristaChange={setFiltroMotorista}
        onPeriodoChange={setPeriodo}
        onLimpar={() => {
          setFiltroVeiculo('')
          setFiltroMotorista('')
          setPeriodo('todos')
        }}
      />

      <TabelaAbastecimentos
        abastecimentos={abastecimentos}
        motorista={motorista}
        mostrarAcoes={mostrarAcoes}
        podeLancar={podeLancar}
        podeExcluir={podeExcluir}
        temFiltro={temFiltro}
        pending={abastecimentosQuery.isPending}
        error={abastecimentosQuery.error}
        isSuccess={abastecimentosQuery.isSuccess}
        total={total}
        onEditar={abrirEdicao}
        onExcluir={setParaExcluir}
      />

      {paraExcluir && (
        <ConfirmDialog
          titulo="Excluir abastecimento"
          mensagem={`O abastecimento de ${formatMoeda(paraExcluir.valor)} do veículo ${paraExcluir.veiculoPlaca}, lançado para ${paraExcluir.motoristaNome}, será removido.`}
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
