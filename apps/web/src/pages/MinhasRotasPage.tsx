import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { rotasApi } from '../api/rotas'
import { veiculosApi } from '../api/veiculos'
import { manutencoesApi } from '../api/manutencoes'
import { mensagensDeErro } from '../api/errors'
import type { AbrirMinhaRotaRequest, EncerrarRotaRequest, RotaResponse } from '../api/types'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { FormDialog, InlineForm, TableStates } from '../components/Table'
import { CheckIcon } from '../components/icons'
import { formatDate, formatKm, hojeInputDate, paraInputDate } from '../lib/format'
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
  const [kmSugerido, setKmSugerido] = useState('')
  const [erros, setErros] = useState<string[]>([])

  const [paraEncerrar, setParaEncerrar] = useState<RotaResponse | null>(null)
  const [formEncerramento, setFormEncerramento] = useState(ENCERRAMENTO_VAZIO)
  const [errosEncerramento, setErrosEncerramento] = useState<string[]>([])

  // Chave própria: o conteúdo é um recorte de `['rotas']`, e as duas telas nunca
  // convivem na mesma sessão — misturá-las serviria dado errado no login seguinte.
  const rotasQuery = useQuery({ queryKey: ['rotas', 'minhas'], queryFn: rotasApi.getMinhas })
  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })
  // Pendências da frota inteira numa consulta só, em vez de uma por veículo escolhido:
  // a lista é curta e o cruzamento é local. Mesma chave da tela de manutenções.
  const pendentesFiltro = { status: 'Pendente' as const }
  const manutencoesQuery = useQuery({
    queryKey: ['manutencoes', pendentesFiltro],
    queryFn: () => manutencoesApi.getAll(pendentesFiltro),
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

  // Só existe uma rota ativa por vez na prática: ela vira o destaque do topo, e o
  // resto é histórico.
  const rotaAtiva = rotas.find((r) => r.ativo) ?? null
  const historico = rotas.filter((r) => !r.ativo)

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
      const podeSubstituir = atual.kmInicial === '' || atual.kmInicial === kmSugerido
      return {
        ...atual,
        codigoVeiculo,
        kmInicial: nova !== '' && podeSubstituir ? nova : atual.kmInicial,
      }
    })
    if (nova !== '') setKmSugerido(nova)
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
    setKmSugerido('')
    setErros([])
    setAberto(true)
  }

  function fecharForm() {
    setAberto(false)
    setForm(FORM_VAZIO)
    setKmSugerido('')
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
            style={{ borderRadius: 0 }}
            onClick={aberto ? fecharForm : abrirCadastro}
            disabled={!aberto && bloqueioNovaRota !== undefined}
            title={bloqueioNovaRota}
          >
            {aberto ? 'Cancelar' : 'Abrir rota'}
          </button>
        }
      />

      {aberto && (
        <InlineForm onSubmit={handleSubmit}>
          <div className="field min-w-[160px] flex-1">
            <label htmlFor="origem">Origem</label>
            <input
              id="origem"
              className="input"
              type="text"
              placeholder="Cidade de origem"
              required
              autoFocus
              style={{ borderRadius: 0 }}
              value={form.origem}
              onChange={(e) => setForm({ ...form, origem: e.target.value })}
            />
          </div>
          <div className="field min-w-[160px] flex-1">
            <label htmlFor="destino">Destino</label>
            <input
              id="destino"
              className="input"
              type="text"
              placeholder="Cidade de destino"
              required
              style={{ borderRadius: 0 }}
              value={form.destino}
              onChange={(e) => setForm({ ...form, destino: e.target.value })}
            />
          </div>
          <div className="field w-[230px]">
            <label htmlFor="codigoVeiculo">Veículo</label>
            <select
              id="codigoVeiculo"
              className="input"
              required
              style={{ borderRadius: 0 }}
              value={form.codigoVeiculo}
              onChange={(e) => aplicarVeiculo(e.target.value)}
            >
              <option value="">Selecione…</option>
              {veiculos.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.placa} — {v.nomeVeiculo} ({formatKm(v.quilometragem)})
                </option>
              ))}
            </select>
          </div>
          {/* A pendência do veículo escolhido, no momento em que ela decide algo:
              antes de sair. Não bloqueia — quem decide é o motorista. */}
          {pendenciasDoVeiculoEscolhido.length > 0 && (
            <div
              className="w-full p-3 text-[13px]"
              style={{
                border: `1px solid ${atrasadasDoVeiculoEscolhido.length > 0 ? 'var(--color-danger)' : 'var(--color-divider)'}`,
                background: 'var(--color-surface)',
              }}
            >
              <strong>
                {atrasadasDoVeiculoEscolhido.length > 0
                  ? 'Este veículo tem manutenção atrasada.'
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
          <div className="field w-[150px]">
            <label htmlFor="dataInicio">Início</label>
            <input
              id="dataInicio"
              className="input"
              type="date"
              required
              style={{ borderRadius: 0 }}
              value={form.dataInicio}
              onChange={(e) => setForm({ ...form, dataInicio: e.target.value })}
            />
          </div>
          <div className="field w-[190px]">
            <label htmlFor="kmInicial">Quilometragem inicial</label>
            <input
              id="kmInicial"
              className="input"
              type="number"
              min={0}
              max={2000000}
              required
              placeholder="0"
              style={{ borderRadius: 0 }}
              value={form.kmInicial}
              onChange={(e) => setForm({ ...form, kmInicial: e.target.value })}
            />
          </div>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ borderRadius: 0, padding: '10px 20px' }}
            disabled={abrirMutation.isPending}
          >
            {abrirMutation.isPending ? 'Abrindo…' : 'Abrir rota'}
          </button>
          <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
            A quilometragem inicial já vem sugerida com o odômetro do veículo. Confira no painel antes
            de sair: ela não pode ser menor que esse número e, se for maior, o odômetro é corrigido
            para o valor informado.
          </p>
          <div className="w-full">
            <ErrorList mensagens={erros} />
          </div>
        </InlineForm>
      )}

      {/* Rota em andamento: o que o motorista precisa ver e agir primeiro. */}
      <section className="mb-8">
        <h2
          className="mb-3 text-[11px] uppercase"
          style={{ fontFamily: 'var(--font-heading)', letterSpacing: '0.08em', color: mutedText }}
        >
          Rota em andamento
        </h2>

        {rotasQuery.isPending && <p className="m-0 text-[13px]" style={{ color: mutedText }}>Carregando suas rotas…</p>}

        {rotasQuery.error && (
          <ErrorList mensagens={mensagensDeErro(rotasQuery.error, 'Não foi possível carregar suas rotas.')} />
        )}

        {rotasQuery.isSuccess && !rotaAtiva && (
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
              onClick={() => abrirEncerramento(rotaAtiva)}
            >
              <CheckIcon size={16} />
              Encerrar rota
            </button>
          </div>
        )}
      </section>

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
              pending={rotasQuery.isPending}
              error={rotasQuery.error}
              empty={rotasQuery.isSuccess && historico.length === 0}
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

      {paraEncerrar && (
        <FormDialog
          titulo="Encerrar rota"
          descricao={`${paraEncerrar.origem} → ${paraEncerrar.destino} — ${descreverVeiculo(
            paraEncerrar.codigoVeiculo,
          )}, aberta em ${formatKm(paraEncerrar.kmInicial)}. Informe o odômetro na chegada: a quilometragem percorrida é a diferença, e o veículo é atualizado com esse número.`}
          textoConfirmar="Encerrar"
          textoPendente="Encerrando…"
          pending={encerrarMutation.isPending}
          erros={errosEncerramento}
          onSubmit={handleEncerrar}
          onCancelar={fecharEncerramento}
        >
          <div className="field w-[190px]">
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
              style={{ borderRadius: 0 }}
              value={formEncerramento.kmFinal}
              onChange={(e) => setFormEncerramento({ ...formEncerramento, kmFinal: e.target.value })}
            />
          </div>
          <div className="field w-[190px]">
            <label htmlFor="dataFim">Data de fim (opcional)</label>
            <input
              id="dataFim"
              className="input"
              type="date"
              // Não pode ser anterior ao início (RN06) nem futura (a API dá margem de
              // 1 dia por causa do fuso). Em branco, a API assume "agora".
              min={paraInputDate(paraEncerrar.dataInicio)}
              max={hojeInputDate()}
              style={{ borderRadius: 0 }}
              value={formEncerramento.dataFim}
              onChange={(e) => setFormEncerramento({ ...formEncerramento, dataFim: e.target.value })}
            />
          </div>
        </FormDialog>
      )}
    </AppLayout>
  )
}
