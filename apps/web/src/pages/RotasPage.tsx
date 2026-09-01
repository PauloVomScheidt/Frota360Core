import { useMemo, useRef, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { rotasApi } from '../api/rotas'
import { motoristasApi } from '../api/motoristas'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import type {
  CriarRotaRequest,
  EncerrarRotaRequest,
  MotoristaResponse,
  RotaResponse,
  VeiculoResponse,
} from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, FormDialog, InlineForm, RowActions, TableStates } from '../components/Table'
import { CheckIcon } from '../components/icons'
import { formatDate, formatKm, hojeInputDate, paraInputDate } from '../lib/format'
import { statusDaRota } from '../lib/rota'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = {
  origem: '',
  destino: '',
  codigoMotorista: '',
  codigoVeiculo: '',
  dataInicio: '',
  kmInicial: '',
}

const ENCERRAMENTO_VAZIO = {
  kmFinal: '',
  dataFim: '',
}

type FormularioRota = typeof FORM_VAZIO
type FormularioEncerramento = typeof ENCERRAMENTO_VAZIO

/** Painel de cadastro/edição — o mesmo formulário para as duas ações, o id decide o verbo. */
function RotaFormulario({
  editando,
  form,
  onFormChange,
  onAplicarVeiculo,
  onSubmit,
  pending,
  erros,
  motoristas,
  veiculos,
}: {
  editando: RotaResponse | null
  form: FormularioRota
  onFormChange: (form: FormularioRota) => void
  onAplicarVeiculo: (codigoVeiculo: string) => void
  onSubmit: (e: FormEvent) => void
  pending: boolean
  erros: string[]
  motoristas: MotoristaResponse[]
  veiculos: VeiculoResponse[]
}) {
  return (
    <InlineForm onSubmit={onSubmit}>
      {editando && (
        <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
          Editando a rota{' '}
          <strong style={{ color: 'var(--color-text)' }}>
            {editando.origem} → {editando.destino}
          </strong>
          .
        </p>
      )}
      <div className="field min-w-[160px] flex-1">
        <label htmlFor="origem">Origem</label>
        <input
          id="origem"
          className="input"
          type="text"
          placeholder="Cidade de origem"
          required
          style={{ borderRadius: 0 }}
          value={form.origem}
          onChange={(e) => onFormChange({ ...form, origem: e.target.value })}
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
          onChange={(e) => onFormChange({ ...form, destino: e.target.value })}
        />
      </div>
      <div className="field w-[200px]">
        <label htmlFor="codigoMotorista">Motorista</label>
        <select
          id="codigoMotorista"
          className="input"
          required
          style={{ borderRadius: 0 }}
          value={form.codigoMotorista}
          onChange={(e) => onFormChange({ ...form, codigoMotorista: e.target.value })}
        >
          <option value="">Selecione…</option>
          {motoristas.map((m) => (
            <option key={m.id} value={m.id}>
              {m.nome}
            </option>
          ))}
        </select>
      </div>
      <div className="field w-[230px]">
        <label htmlFor="codigoVeiculo">Veículo</label>
        <select
          id="codigoVeiculo"
          className="input"
          required
          style={{ borderRadius: 0 }}
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
      <div className="field w-[150px]">
        <label htmlFor="dataInicio">Início</label>
        <input
          id="dataInicio"
          className="input"
          type="date"
          required
          style={{ borderRadius: 0 }}
          value={form.dataInicio}
          onChange={(e) => onFormChange({ ...form, dataInicio: e.target.value })}
        />
      </div>
      {/* Hodômetro de abertura: só na criação — o PUT não altera esse número. */}
      {!editando && (
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
            onChange={(e) => onFormChange({ ...form, kmInicial: e.target.value })}
          />
        </div>
      )}
      <button
        type="submit"
        className="btn btn-primary"
        style={{ borderRadius: 0, padding: '10px 20px' }}
        disabled={pending}
      >
        {pending ? 'Salvando…' : editando ? 'Salvar alterações' : 'Cadastrar'}
      </button>
      {!editando && (
        <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
          A quilometragem inicial já vem sugerida com o odômetro atual do veículo. Ela não pode ser
          menor que esse número — e, se for maior, o odômetro do veículo é atualizado já na abertura
          da rota. O encerramento é feito depois, pela ação "Encerrar" na linha da rota.
        </p>
      )}
      <div className="w-full">
        <ErrorList mensagens={erros} />
      </div>
    </InlineForm>
  )
}

/** A tabela — situação de cada rota e as ações por linha (encerrar/editar/excluir). */
function TabelaRotas({
  rotas,
  veiculoPorId,
  colunas,
  mostrarAcoes,
  podeCadastrar,
  podeExcluir,
  pending,
  error,
  isSuccess,
  onEncerrar,
  onEditar,
  onExcluir,
}: {
  rotas: RotaResponse[]
  veiculoPorId: Map<number, VeiculoResponse>
  colunas: number
  mostrarAcoes: boolean
  podeCadastrar: boolean
  podeExcluir: boolean
  pending: boolean
  error: unknown
  isSuccess: boolean
  onEncerrar: (rota: RotaResponse) => void
  onEditar: (rota: RotaResponse) => void
  onExcluir: (rota: RotaResponse) => void
}) {
  return (
    <div className="overflow-x-auto">
      <table className="table">
        <thead>
          <tr>
            <th>Origem → Destino</th>
            <th>Motorista</th>
            <th>Veículo</th>
            <th>Início</th>
            <th>Fim</th>
            <th>Quilometragem</th>
            <th>Status</th>
            {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
          </tr>
        </thead>
        <tbody>
          <TableStates
            colSpan={colunas}
            pending={pending}
            error={error}
            empty={isSuccess && rotas.length === 0}
            textoCarregando="Carregando rotas…"
            textoErro="Não foi possível carregar as rotas."
            textoVazio="Nenhuma rota cadastrada ainda."
          />
          {rotas.map((rota) => {
            const status = statusDaRota(rota)
            return (
              <tr key={rota.id}>
                <td className="font-semibold">
                  {rota.origem} → {rota.destino}
                </td>
                {/* Desnormalizado: um motorista rebaixado sai da lista, mas a rota
                    dele continua identificada. */}
                <td>{rota.nomeMotorista ?? `#${rota.codigoMotorista}`}</td>
                <td>{veiculoPorId.get(rota.codigoVeiculo)?.placa ?? `#${rota.codigoVeiculo}`}</td>
                <td>{formatDate(rota.dataInicio)}</td>
                <td>{formatDate(rota.dataFim)}</td>
                <td>
                  {/* `kmPercorrido` só existe depois do encerramento; antes, mostramos a abertura. */}
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
                {mostrarAcoes && (
                  <td>
                    <div className="flex items-center justify-end gap-1">
                      {/* Encerrar só vale em rota ativa — o resto é histórico. */}
                      {podeCadastrar && rota.ativo && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          style={{ borderRadius: 0, padding: '6px 12px', fontSize: 12 }}
                          onClick={() => onEncerrar(rota)}
                        >
                          <CheckIcon size={14} />
                          Encerrar
                        </button>
                      )}
                      <RowActions
                        descricao={`a rota ${rota.origem} → ${rota.destino}`}
                        onEditar={podeCadastrar ? () => onEditar(rota) : undefined}
                        onExcluir={podeExcluir ? () => onExcluir(rota) : undefined}
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

/** Diálogo de encerramento — quilometragem final e data de chegada. */
function EncerramentoRotaFormulario({
  paraEncerrar,
  placa,
  form,
  onFormChange,
  onSubmit,
  onCancelar,
  pending,
  erros,
}: {
  paraEncerrar: RotaResponse
  placa: string
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
      descricao={`${paraEncerrar.origem} → ${paraEncerrar.destino} — ${placa}, aberta em ${formatKm(paraEncerrar.kmInicial)}. A quilometragem percorrida é calculada pela diferença, e o odômetro do veículo é atualizado quando o km final for maior que o atual.`}
      textoConfirmar="Encerrar"
      textoPendente="Encerrando…"
      pending={pending}
      erros={erros}
      onSubmit={onSubmit}
      onCancelar={onCancelar}
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
          value={form.kmFinal}
          onChange={(e) => onFormChange({ ...form, kmFinal: e.target.value })}
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
          value={form.dataFim}
          onChange={(e) => onFormChange({ ...form, dataFim: e.target.value })}
        />
      </div>
    </FormDialog>
  )
}

export function RotasPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarRotas(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<RotaResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  // Guarda a última sugestão de km para não sobrescrever um valor digitado à mão.
  // Nunca aparece na tela — só é lido/escrito dentro de handlers — então é ref, não
  // state: mudar esse valor não precisa redesenhar o componente.
  const kmSugeridoRef = useRef('')
  const [erros, setErros] = useState<string[]>([])

  const [paraEncerrar, setParaEncerrar] = useState<RotaResponse | null>(null)
  const [formEncerramento, setFormEncerramento] = useState(ENCERRAMENTO_VAZIO)
  const [errosEncerramento, setErrosEncerramento] = useState<string[]>([])

  const [paraExcluir, setParaExcluir] = useState<RotaResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  const rotasQuery = useQuery({ queryKey: ['rotas'], queryFn: rotasApi.getAll })
  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })
  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })

  const motoristas = motoristasQuery.data ?? []
  const veiculos = veiculosQuery.data ?? []
  const rotas = rotasQuery.data ?? []

  // O nome do motorista vem desnormalizado na resposta; só a placa precisa de
  // cruzamento. Memoizado sobre `*.data` (e não sobre o array com fallback `?? []`,
  // que muda de identidade a cada render); o mapa também alimenta a sugestão de km.
  const veiculoPorId = useMemo(
    () => new Map((veiculosQuery.data ?? []).map((v) => [v.id, v])),
    [veiculosQuery.data],
  )

  /**
   * Troca de veículo sugere o odômetro atual dele como quilometragem de abertura,
   * exceto se o operador já digitou um km próprio.
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

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  // Só o POST leva `kmInicial` — o PUT não altera o hodômetro de abertura.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: CriarRotaRequest }) =>
      id === null
        ? rotasApi.create(body)
        : rotasApi.update(id, {
            origem: body.origem,
            destino: body.destino,
            codigoMotorista: body.codigoMotorista,
            codigoVeiculo: body.codigoVeiculo,
            dataInicio: body.dataInicio,
          }),
    onSuccess: (_, { id }) => {
      fecharForm()
      queryClient.invalidateQueries({ queryKey: ['rotas'] })
      // Abrir rota com `kmInicial` acima do odômetro avança o veículo (o veículo rodou
      // fora do sistema), e o odômetro é o que define `atrasada`/`kmRestantes`.
      if (id === null) {
        queryClient.invalidateQueries({ queryKey: ['veiculos'] })
        queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
      }
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível cadastrar a rota.',
        ),
      ),
  })

  const encerrarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number; body: EncerrarRotaRequest }) =>
      rotasApi.encerrar(id, body),
    onSuccess: () => {
      fecharEncerramento()
      queryClient.invalidateQueries({ queryKey: ['rotas'] })
      // Encerrar avança o odômetro do veículo, e é dele que dependem `atrasada` e
      // `kmRestantes` das manutenções. A cadeia é rota → veículo → manutenção.
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
      queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
      // E é aqui que o `kmPercorrido` é apurado — o denominador do custo por km.
      queryClient.invalidateQueries({ queryKey: ['custos'] })
    },
    onError: (error) =>
      setErrosEncerramento(mensagensDeErro(error, 'Não foi possível encerrar a rota.')),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => rotasApi.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      queryClient.invalidateQueries({ queryKey: ['rotas'] })
      // Excluir uma rota aberta solta o veículo: a coluna "Situação" de /veiculos
      // ficaria dizendo "Em rota" até o próximo staleTime.
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
      // Se a rota estava encerrada, o `kmPercorrido` dela some do custo por km.
      queryClient.invalidateQueries({ queryKey: ['custos'] })
    },
    onError: (error) => setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir a rota.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    salvarMutation.mutate({
      id: editando?.id ?? null,
      body: {
        origem: form.origem,
        destino: form.destino,
        codigoMotorista: Number(form.codigoMotorista),
        codigoVeiculo: Number(form.codigoVeiculo),
        dataInicio: form.dataInicio,
        kmInicial: Number(form.kmInicial),
      },
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
    setEditando(null)
    setForm(FORM_VAZIO)
    kmSugeridoRef.current = ''
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(rota: RotaResponse) {
    setEditando(rota)
    setForm({
      origem: rota.origem,
      destino: rota.destino,
      codigoMotorista: String(rota.codigoMotorista),
      codigoVeiculo: String(rota.codigoVeiculo),
      dataInicio: paraInputDate(rota.dataInicio),
      // Não vai no PUT, mas o estado precisa do campo — a edição nem o exibe.
      kmInicial: String(rota.kmInicial),
    })
    kmSugeridoRef.current = ''
    setErros([])
    setAberto(true)
    // O formulário abre acima da tabela — a linha editada pode estar fora da tela.
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function fecharForm() {
    setAberto(false)
    setEditando(null)
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

  const semCadastrosBase =
    (motoristasQuery.isSuccess && motoristas.length === 0) ||
    (veiculosQuery.isSuccess && veiculos.length === 0)

  const mostrarAcoes = podeCadastrar || podeExcluir
  const colunas = mostrarAcoes ? 8 : 7

  return (
    <AppLayout>
      <PageHeader
        titulo="Rotas"
        subtitulo="Rotas cadastradas e sua situação atual."
        acoes={
          podeCadastrar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
              disabled={semCadastrosBase && !aberto}
              title={semCadastrosBase ? 'Cadastre ao menos um motorista e um veículo antes.' : undefined}
            >
              {aberto ? 'Cancelar' : 'Nova rota'}
            </button>
          )
        }
      />

      {aberto && podeCadastrar && (
        <RotaFormulario
          editando={editando}
          form={form}
          onFormChange={setForm}
          onAplicarVeiculo={aplicarVeiculo}
          onSubmit={handleSubmit}
          pending={salvarMutation.isPending}
          erros={erros}
          motoristas={motoristas}
          veiculos={veiculos}
        />
      )}

      <TabelaRotas
        rotas={rotas}
        veiculoPorId={veiculoPorId}
        colunas={colunas}
        mostrarAcoes={mostrarAcoes}
        podeCadastrar={podeCadastrar}
        podeExcluir={podeExcluir}
        pending={rotasQuery.isPending}
        error={rotasQuery.error}
        isSuccess={rotasQuery.isSuccess}
        onEncerrar={abrirEncerramento}
        onEditar={abrirEdicao}
        onExcluir={(rota) => {
          setErrosExclusao([])
          setParaExcluir(rota)
        }}
      />

      {paraEncerrar && (
        <EncerramentoRotaFormulario
          paraEncerrar={paraEncerrar}
          placa={veiculoPorId.get(paraEncerrar.codigoVeiculo)?.placa ?? `#${paraEncerrar.codigoVeiculo}`}
          form={formEncerramento}
          onFormChange={setFormEncerramento}
          onSubmit={handleEncerrar}
          onCancelar={fecharEncerramento}
          pending={encerrarMutation.isPending}
          erros={errosEncerramento}
        />
      )}

      {paraExcluir && (
        <ConfirmDialog
          titulo="Excluir rota"
          mensagem={`A rota ${paraExcluir.origem} → ${paraExcluir.destino} será removida junto com o histórico dela. Esta ação não pode ser desfeita.`}
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
