import { useMemo, useRef, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { rotasApi } from '../api/rotas'
import { veiculosApi } from '../api/veiculos'
import { manutencoesApi } from '../api/manutencoes'
import { mensagensDeErro } from '../api/errors'
import type {
  AbrirMinhaRotaRequest,
  EncerrarRotaRequest,
  ManutencaoResponse,
  RotaResponse,
  VeiculoResponse,
} from '../api/types'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { FormDialog, Paginacao, SecaoCampos, TableStates } from '../components/Table'
import { usePaginacao } from '../lib/paginacao'
import { CheckIcon } from '../components/icons'
import { formatDate, formatKm, hojeInputDate, paraInputDate } from '../lib/format'
import { estaVencendo } from '../lib/manutencao'
import { statusDaRota } from '../lib/rota'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

// Sem `codigoMotorista`: quem decide o motorista é o servidor, pela claim do JWT.
const FORM_VAZIO = {
  origem: '',
  destino: '',
  codigoVeiculo: '',
  dataInicio: '',
  kmInicial: '',
}

const ENCERRAMENTO_VAZIO = {
  kmFinal: '',
  dataFim: '',
}

// Pendências da frota inteira numa consulta só, em vez de uma por veículo escolhido:
// a lista é curta e o cruzamento é local. Mesma chave da tela de manutenções.
const FILTRO_PENDENTES = { status: 'Pendente' as const }

type FormularioMinhaRota = typeof FORM_VAZIO
type FormularioEncerramento = typeof ENCERRAMENTO_VAZIO

/** Modal de abertura — veículo, data e a pendência de manutenção do veículo escolhido. */
function MinhaRotaFormulario({
  form,
  onFormChange,
  onAplicarVeiculo,
  onSubmit,
  onCancelar,
  pending,
  erros,
  veiculos,
  pendenciasDoVeiculoEscolhido,
  atrasadasDoVeiculoEscolhido,
  vencendoDoVeiculoEscolhido,
  corDoAlerta,
}: {
  form: FormularioMinhaRota
  onFormChange: (form: FormularioMinhaRota) => void
  onAplicarVeiculo: (codigoVeiculo: string) => void
  onSubmit: (e: FormEvent) => void
  onCancelar: () => void
  pending: boolean
  erros: string[]
  veiculos: VeiculoResponse[]
  pendenciasDoVeiculoEscolhido: ManutencaoResponse[]
  atrasadasDoVeiculoEscolhido: ManutencaoResponse[]
  vencendoDoVeiculoEscolhido: ManutencaoResponse[]
  corDoAlerta: string
}) {
  return (
    <FormDialog
      titulo="Abrir rota"
      textoConfirmar="Abrir rota"
      textoPendente="Abrindo…"
      largura={760}
      pending={pending}
      erros={erros}
      onSubmit={onSubmit}
      onCancelar={onCancelar}
    >
      <SecaoCampos titulo="Trajeto">
        <div className="field">
          <label htmlFor="origem">Origem</label>
          <input
            id="origem"
            className="input"
            type="text"
            placeholder="Cidade de origem"
            required
            autoFocus
            value={form.origem}
            onChange={(e) => onFormChange({ ...form, origem: e.target.value })}
          />
        </div>
        <div className="field">
          <label htmlFor="destino">Destino</label>
          <input
            id="destino"
            className="input"
            type="text"
            placeholder="Cidade de destino"
            required
            value={form.destino}
            onChange={(e) => onFormChange({ ...form, destino: e.target.value })}
          />
        </div>
        <div className="field">
          <label htmlFor="dataInicio">Início</label>
          <input
            id="dataInicio"
            className="input"
            type="date"
            required
            value={form.dataInicio}
            onChange={(e) => onFormChange({ ...form, dataInicio: e.target.value })}
          />
        </div>
      </SecaoCampos>

      <SecaoCampos titulo="Veículo">
        <div className="field">
          <label htmlFor="codigoVeiculo">Veículo</label>
          <select
            id="codigoVeiculo"
            className="input"
            required
            value={form.codigoVeiculo}
            onChange={(e) => onAplicarVeiculo(e.target.value)}
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
          <label htmlFor="kmInicial">Quilometragem inicial</label>
          <input
            id="kmInicial"
            className="input"
            type="number"
            min={0}
            max={2000000}
            required
            placeholder="0"
            value={form.kmInicial}
            onChange={(e) => onFormChange({ ...form, kmInicial: e.target.value })}
          />
        </div>
        {/* A pendência do veículo escolhido, no momento em que ela decide algo:
            antes de sair. Não bloqueia — quem decide é o motorista. */}
        {pendenciasDoVeiculoEscolhido.length > 0 && (
          <div
            className="campo-largo p-3 text-[13px]"
            style={{
              border: `1px solid ${corDoAlerta}`,
              background: 'var(--color-surface)',
            }}
          >
            <strong>
              {atrasadasDoVeiculoEscolhido.length > 0
                ? 'Este veículo tem manutenção atrasada.'
                : vencendoDoVeiculoEscolhido.length > 0
                  ? 'Este veículo tem manutenção vencendo.'
                  : 'Este veículo tem manutenção prevista.'}
            </strong>
            <ul className="mt-1 mb-0 list-none p-0">
              {pendenciasDoVeiculoEscolhido.map((m) => (
                <li key={m.id} style={{ color: mutedText }}>
                  {m.tipoManutencaoNome} · prevista em {formatKm(m.quilometragemPrevista)}
                  {m.atrasada
                    ? ' · atrasada'
                    : m.kmRestantes != null
                      ? ` · faltam ${formatKm(m.kmRestantes)}`
                      : ''}
                </li>
              ))}
            </ul>
          </div>
        )}
        <p className="campo-largo m-0 text-[13px]" style={{ color: mutedText }}>
          A quilometragem inicial já vem sugerida com o odômetro do veículo. Confira no painel
          antes de sair: ela não pode ser menor que esse número e, se for maior, o odômetro é
          corrigido para o valor informado.
        </p>
      </SecaoCampos>
    </FormDialog>
  )
}

/** Rota em andamento: o que o motorista precisa ver e agir primeiro. */
function RotaAtivaCard({
  pending,
  error,
  isSuccess,
  rotaAtiva,
  descreverVeiculo,
  onEncerrar,
}: {
  pending: boolean
  error: Error | null
  isSuccess: boolean
  rotaAtiva: RotaResponse | null
  descreverVeiculo: (codigoVeiculo: number) => string
  onEncerrar: (rota: RotaResponse) => void
}) {
  return (
    <section className="mb-8">
      <h2
        className="mb-3 text-[11px] uppercase"
        style={{ fontFamily: 'var(--font-heading)', letterSpacing: '0.08em', color: mutedText }}
      >
        Rota em andamento
      </h2>

      {pending && <p className="m-0 text-[13px]" style={{ color: mutedText }}>Carregando suas rotas…</p>}

      {error && <ErrorList mensagens={mensagensDeErro(error, 'Não foi possível carregar suas rotas.')} />}

      {isSuccess && !rotaAtiva && (
        <p className="m-0 text-[13px]" style={{ color: mutedText }}>
          Nenhuma rota aberta no momento. Use "Abrir rota" ao iniciar a viagem.
        </p>
      )}

      {rotaAtiva && (
        <div
          className="flex flex-wrap items-end justify-between gap-6 p-5"
          style={{ border: '2px solid var(--color-text)', background: 'var(--color-surface)' }}
        >
          <div className="min-w-[240px]">
            <div className="mb-2 flex items-center gap-3">
              <span className="text-lg font-bold">
                {rotaAtiva.origem} → {rotaAtiva.destino}
              </span>
              <span className={statusDaRota(rotaAtiva).classe}>{statusDaRota(rotaAtiva).rotulo}</span>
            </div>
            <div className="text-[13px]" style={{ color: mutedText }}>
              {descreverVeiculo(rotaAtiva.codigoVeiculo)} · aberta em {formatDate(rotaAtiva.dataInicio)} ·
              saiu com {formatKm(rotaAtiva.kmInicial)}
            </div>
          </div>
          <button
            type="button"
            className="btn btn-primary"
            style={{ borderRadius: 0, padding: '12px 24px' }}
            onClick={() => onEncerrar(rotaAtiva)}
          >
            <CheckIcon size={16} />
            Encerrar rota
          </button>
        </div>
      )}
    </section>
  )
}

/** Histórico — rotas já encerradas, mais recentes primeiro (ordem que a API já devolve). */
function HistoricoRotasTabela({
  historico,
  veiculoPorId,
  pending,
  error,
  isSuccess,
}: {
  historico: RotaResponse[]
  veiculoPorId: Map<number, VeiculoResponse>
  pending: boolean
  error: unknown
  isSuccess: boolean
}) {
  return (
    <>
      <h2
        className="mb-3 text-[11px] uppercase"
        style={{ fontFamily: 'var(--font-heading)', letterSpacing: '0.08em', color: mutedText }}
      >
        Histórico
      </h2>

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Origem → Destino</th>
              <th>Veículo</th>
              <th>Início</th>
              <th>Fim</th>
              <th>Quilometragem</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={6}
              pending={pending}
              error={error}
              empty={isSuccess && historico.length === 0}
              textoCarregando="Carregando suas rotas…"
              textoErro="Não foi possível carregar suas rotas."
              textoVazio="Você ainda não encerrou nenhuma rota."
            />
            {historico.map((rota) => {
              const status = statusDaRota(rota)
              return (
                <tr key={rota.id}>
                  <td className="font-semibold">
                    {rota.origem} → {rota.destino}
                  </td>
                  <td>{veiculoPorId.get(rota.codigoVeiculo)?.placa ?? `#${rota.codigoVeiculo}`}</td>
                  <td>{formatDate(rota.dataInicio)}</td>
                  <td>{formatDate(rota.dataFim)}</td>
                  <td>
                    {/* `kmPercorrido` só existe depois do encerramento. */}
                    <div>{rota.kmPercorrido != null ? formatKm(rota.kmPercorrido) : '—'}</div>
                    <div className="text-[12px]" style={{ color: mutedText }}>
                      {rota.kmFinal != null
                        ? `${formatKm(rota.kmInicial)} → ${formatKm(rota.kmFinal)}`
                        : `desde ${formatKm(rota.kmInicial)}`}
                    </div>
                  </td>
                  <td>
                    <span className={status.classe}>{status.rotulo}</span>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </>
  )
}

/** Diálogo de encerramento — quilometragem final e data de chegada. */
function EncerramentoMinhaRotaFormulario({
  paraEncerrar,
  descricaoVeiculo,
  form,
  onFormChange,
  onSubmit,
  onCancelar,
  pending,
  erros,
}: {
  paraEncerrar: RotaResponse
  descricaoVeiculo: string
  form: FormularioEncerramento
  onFormChange: (form: FormularioEncerramento) => void
  onSubmit: (e: FormEvent) => void
  onCancelar: () => void
  pending: boolean
  erros: string[]
}) {
  return (
    <FormDialog
      titulo="Encerrar rota"
      descricao={`${paraEncerrar.origem} → ${paraEncerrar.destino} — ${descricaoVeiculo}, aberta em ${formatKm(paraEncerrar.kmInicial)}. Informe o odômetro na chegada: a quilometragem percorrida é a diferença, e o veículo é atualizado com esse número.`}
      textoConfirmar="Encerrar"
      textoPendente="Encerrando…"
      pending={pending}
      erros={erros}
      onSubmit={onSubmit}
      onCancelar={onCancelar}
    >
      <SecaoCampos>
        <div className="field">
          <label htmlFor="kmFinal">Quilometragem final</label>
          <input
            id="kmFinal"
            className="input"
            type="number"
            // Não pode ser menor que a abertura — a API recusa com 422.
            min={paraEncerrar.kmInicial}
            max={2000000}
            required
            autoFocus
            value={form.kmFinal}
            onChange={(e) => onFormChange({ ...form, kmFinal: e.target.value })}
          />
        </div>
        <div className="field">
          <label htmlFor="dataFim">Data de fim (opcional)</label>
          <input
            id="dataFim"
            className="input"
            type="date"
            // Não pode ser anterior ao início (RN06) nem futura (a API dá margem de
            // 1 dia por causa do fuso). Em branco, a API assume "agora".
            min={paraInputDate(paraEncerrar.dataInicio)}
            max={hojeInputDate()}
            value={form.dataFim}
            onChange={(e) => onFormChange({ ...form, dataFim: e.target.value })}
          />
        </div>
      </SecaoCampos>
    </FormDialog>
  )
}

/**
 * Tela do motorista (`/minhas-rotas`). Só as rotas atribuídas a ele, e só as três
 * ações do dia a dia: olhar, abrir e encerrar. Editar e excluir continuam sendo da
 * gestão — a API responde 403 nesses verbos para esta role.
 *
 * O recorte por motorista é do servidor (`GET /rota/minhas` lê a claim); aqui não
 * há filtro nenhum, e nem poderia haver — filtro de cliente não é isolamento.
 */
export function MinhasRotasPage() {
  const queryClient = useQueryClient()

  const [aberto, setAberto] = useState(false)
  const [form, setForm] = useState(FORM_VAZIO)
  // Guarda a última sugestão de km para não sobrescrever um valor digitado à mão.
  // Nunca aparece na tela — só é lido/escrito dentro de handlers — então é ref, não
  // state: mudar esse valor não precisa redesenhar o componente.
  const kmSugeridoRef = useRef('')
  const [erros, setErros] = useState<string[]>([])

  const [paraEncerrar, setParaEncerrar] = useState<RotaResponse | null>(null)
  const [formEncerramento, setFormEncerramento] = useState(ENCERRAMENTO_VAZIO)
  const [errosEncerramento, setErrosEncerramento] = useState<string[]>([])

  // Chave própria: o conteúdo é um recorte de `['rotas']`, e as duas telas nunca
  // convivem na mesma sessão — misturá-las serviria dado errado no login seguinte.
  const rotasQuery = useQuery({ queryKey: ['rotas', 'minhas'], queryFn: rotasApi.getMinhas })
  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })
  const manutencoesQuery = useQuery({
    queryKey: ['manutencoes', FILTRO_PENDENTES],
    queryFn: () => manutencoesApi.getAll(FILTRO_PENDENTES),
  })

  const rotas = rotasQuery.data ?? []
  const veiculos = veiculosQuery.data ?? []
  const manutencoes = manutencoesQuery.data ?? []

  // A resposta traz só o código do veículo — o cruzamento com placa/odômetro é aqui.
  const veiculoPorId = useMemo(
    () => new Map((veiculosQuery.data ?? []).map((v) => [v.id, v])),
    [veiculosQuery.data],
  )

  // Pendências por veículo, com as atrasadas primeiro — é a que muda a decisão de sair.
  const pendenciasPorVeiculo = useMemo(() => {
    const mapa = new Map<number, typeof manutencoes>()
    for (const m of manutencoesQuery.data ?? []) {
      const lista = mapa.get(m.veiculoId) ?? []
      lista.push(m)
      mapa.set(m.veiculoId, lista)
    }
    for (const lista of mapa.values()) {
      lista.sort((a, b) => Number(b.atrasada) - Number(a.atrasada))
    }
    return mapa
  }, [manutencoesQuery.data])

  const pendenciasDoVeiculoEscolhido = pendenciasPorVeiculo.get(Number(form.codigoVeiculo)) ?? []
  const atrasadasDoVeiculoEscolhido = pendenciasDoVeiculoEscolhido.filter((m) => m.atrasada)
  const vencendoDoVeiculoEscolhido = pendenciasDoVeiculoEscolhido.filter(estaVencendo)

  // Mesma escala das tags de /manutencoes: vermelho quando já venceu, âmbar quando está
  // na faixa de aviso, contorno neutro quando a pendência ainda é distante.
  const corDoAlerta =
    atrasadasDoVeiculoEscolhido.length > 0
      ? 'var(--color-danger)'
      : vencendoDoVeiculoEscolhido.length > 0
        ? 'var(--color-warning)'
        : 'var(--color-divider)'

  // Só existe uma rota ativa por vez na prática: ela vira o destaque do topo, e o
  // resto é histórico.
  const rotaAtiva = rotas.find((r) => r.ativo) ?? null
  const historico = rotas.filter((r) => !r.ativo)
  const p = usePaginacao(historico)

  function descreverVeiculo(codigoVeiculo: number): string {
    const veiculo = veiculoPorId.get(codigoVeiculo)
    return veiculo ? `${veiculo.placa} — ${veiculo.nomeVeiculo}` : `#${codigoVeiculo}`
  }

  /**
   * Troca de veículo sugere o odômetro atual dele como quilometragem de abertura,
   * exceto se o motorista já digitou um km próprio.
   */
  function aplicarVeiculo(codigoVeiculo: string) {
    const veiculo = veiculoPorId.get(Number(codigoVeiculo))
    const nova = veiculo ? String(veiculo.quilometragem) : ''
    setForm((atual) => {
      const podeSubstituir = atual.kmInicial === '' || atual.kmInicial === kmSugeridoRef.current
      return {
        ...atual,
        codigoVeiculo,
        kmInicial: nova !== '' && podeSubstituir ? nova : atual.kmInicial,
      }
    })
    if (nova !== '') kmSugeridoRef.current = nova
  }

  const abrirMutation = useMutation({
    mutationFn: (body: AbrirMinhaRotaRequest) => rotasApi.abrirMinha(body),
    onSuccess: () => {
      fecharForm()
      queryClient.invalidateQueries({ queryKey: ['rotas', 'minhas'] })
      // Abrir rota com `kmInicial` acima do odômetro avança o veículo (ele rodou fora
      // do sistema), e o odômetro é o que define `atrasada`/`kmRestantes` — a cadeia
      // rota → veículo → manutenção vale aqui como na tela de gestão.
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
      queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível abrir a rota.')),
  })

  const encerrarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number; body: EncerrarRotaRequest }) =>
      rotasApi.encerrar(id, body),
    onSuccess: () => {
      fecharEncerramento()
      queryClient.invalidateQueries({ queryKey: ['rotas', 'minhas'] })
      // Encerrar avança o odômetro do veículo, grava a ficha de última viagem e pode
      // acender uma manutenção atrasada. A cadeia é rota → veículo → manutenção.
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
      queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
      // O motorista não vê `/custos`, mas apura aqui o `kmPercorrido` que a gestão divide
      // pelo custo — e o cache é o mesmo se ela abrir a tela na sequência.
      queryClient.invalidateQueries({ queryKey: ['custos'] })
    },
    onError: (error) =>
      setErrosEncerramento(mensagensDeErro(error, 'Não foi possível encerrar a rota.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    abrirMutation.mutate({
      origem: form.origem,
      destino: form.destino,
      codigoVeiculo: Number(form.codigoVeiculo),
      dataInicio: form.dataInicio,
      kmInicial: Number(form.kmInicial),
    })
  }

  function handleEncerrar(e: FormEvent) {
    e.preventDefault()
    if (!paraEncerrar) return
    encerrarMutation.mutate({
      id: paraEncerrar.id,
      body: {
        kmFinal: Number(formEncerramento.kmFinal),
        // Em branco a API assume "agora".
        dataFim: formEncerramento.dataFim || null,
      },
    })
  }

  function abrirCadastro() {
    setForm({ ...FORM_VAZIO, dataInicio: hojeInputDate() })
    kmSugeridoRef.current = ''
    setErros([])
    setAberto(true)
  }

  function fecharForm() {
    setAberto(false)
    setForm(FORM_VAZIO)
    kmSugeridoRef.current = ''
    setErros([])
  }

  function abrirEncerramento(rota: RotaResponse) {
    setParaEncerrar(rota)
    setFormEncerramento({
      // O odômetro atual do veículo é o palpite mais próximo do real; sem o veículo
      // em cache, o km de abertura é o mínimo aceito pela API.
      kmFinal: String(veiculoPorId.get(rota.codigoVeiculo)?.quilometragem ?? rota.kmInicial),
      dataFim: hojeInputDate(),
    })
    setErrosEncerramento([])
  }

  function fecharEncerramento() {
    setParaEncerrar(null)
    setFormEncerramento(ENCERRAMENTO_VAZIO)
    setErrosEncerramento([])
  }

  const semVeiculos = veiculosQuery.isSuccess && veiculos.length === 0
  // Uma rota por vez: com uma aberta, o caminho é encerrá-la, não empilhar outra.
  const bloqueioNovaRota = rotaAtiva
    ? 'Encerre a rota em andamento antes de abrir outra.'
    : semVeiculos
      ? 'Nenhum veículo cadastrado — fale com o supervisor.'
      : undefined

  return (
    <AppLayout>
      <PageHeader
        titulo="Minhas rotas"
        subtitulo="As rotas atribuídas a você. Abra ao sair e encerre ao chegar."
        acoes={
          <button
            type="button"
            className="btn btn-primary"
            onClick={abrirCadastro}
            disabled={bloqueioNovaRota !== undefined}
            title={bloqueioNovaRota}
          >
            Abrir rota
          </button>
        }
      />

      {aberto && (
        <MinhaRotaFormulario
          form={form}
          onFormChange={setForm}
          onAplicarVeiculo={aplicarVeiculo}
          onSubmit={handleSubmit}
          onCancelar={fecharForm}
          pending={abrirMutation.isPending}
          erros={erros}
          veiculos={veiculos}
          pendenciasDoVeiculoEscolhido={pendenciasDoVeiculoEscolhido}
          atrasadasDoVeiculoEscolhido={atrasadasDoVeiculoEscolhido}
          vencendoDoVeiculoEscolhido={vencendoDoVeiculoEscolhido}
          corDoAlerta={corDoAlerta}
        />
      )}

      <RotaAtivaCard
        pending={rotasQuery.isPending}
        error={rotasQuery.error}
        isSuccess={rotasQuery.isSuccess}
        rotaAtiva={rotaAtiva}
        descreverVeiculo={descreverVeiculo}
        onEncerrar={abrirEncerramento}
      />

      <HistoricoRotasTabela
        historico={p.itensDaPagina}
        veiculoPorId={veiculoPorId}
        pending={rotasQuery.isPending}
        error={rotasQuery.error}
        isSuccess={rotasQuery.isSuccess}
      />

      <Paginacao {...p} pending={rotasQuery.isFetching} />

      {paraEncerrar && (
        <EncerramentoMinhaRotaFormulario
          paraEncerrar={paraEncerrar}
          descricaoVeiculo={descreverVeiculo(paraEncerrar.codigoVeiculo)}
          form={formEncerramento}
          onFormChange={setFormEncerramento}
          onSubmit={handleEncerrar}
          onCancelar={fecharEncerramento}
          pending={encerrarMutation.isPending}
          erros={errosEncerramento}
        />
      )}
    </AppLayout>
  )
}
