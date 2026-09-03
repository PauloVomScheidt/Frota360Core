import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { despesasApi } from '../api/despesas'
import { tiposDespesaApi } from '../api/tiposDespesa'
import { motoristasApi } from '../api/motoristas'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import type {
  DespesaFiltro,
  DespesaRequest,
  DespesaResponse,
  MotoristaResponse,
  TipoDespesaResponse,
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
  tipoDespesaId: '',
  motoristaId: '',
  valor: '',
  dataDespesa: hojeInputDate(),
  observacao: '',
}

type FormDespesa = typeof FORM_VAZIO

function DespesaFormulario({
  editando,
  form,
  onFormChange,
  onSubmit,
  pending,
  erros,
  veiculos,
  tipos,
  motoristas,
}: {
  editando: DespesaResponse | null
  form: FormDespesa
  onFormChange: (form: FormDespesa) => void
  onSubmit: (e: FormEvent) => void
  pending: boolean
  erros: string[]
  veiculos: VeiculoResponse[]
  tipos: TipoDespesaResponse[]
  motoristas: MotoristaResponse[]
}) {
  return (
    <InlineForm onSubmit={onSubmit}>
      {editando && (
        <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
          Corrigindo a despesa de{' '}
          <strong style={{ color: 'var(--color-text)' }}>{formatMoeda(editando.valor)}</strong> em{' '}
          {formatDate(editando.dataDespesa)}.
        </p>
      )}

      <div className="field w-[230px]">
        <label htmlFor="veiculoDespesa">Veículo</label>
        <select
          id="veiculoDespesa"
          className="input"
          required
          style={{ borderRadius: 0 }}
          value={form.veiculoId}
          onChange={(e) => onFormChange({ ...form, veiculoId: e.target.value })}
        >
          <option value="">Selecione…</option>
          {veiculos.map((v) => (
            <option key={v.id} value={v.id}>
              {v.placa} — {v.nomeVeiculo}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[200px]">
        <label htmlFor="tipoDespesa">Tipo</label>
        <select
          id="tipoDespesa"
          className="input"
          required
          style={{ borderRadius: 0 }}
          value={form.tipoDespesaId}
          onChange={(e) => onFormChange({ ...form, tipoDespesaId: e.target.value })}
        >
          <option value="">Selecione…</option>
          {tipos.map((t) => (
            <option key={t.id} value={t.id}>
              {t.nome}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[210px]">
        <label htmlFor="motoristaDespesa">Motorista (opcional)</label>
        <select
          id="motoristaDespesa"
          className="input"
          style={{ borderRadius: 0 }}
          value={form.motoristaId}
          onChange={(e) => onFormChange({ ...form, motoristaId: e.target.value })}
        >
          <option value="">Não atribuída</option>
          {motoristas.map((m) => (
            <option key={m.id} value={m.id}>
              {m.nome}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[150px]">
        <label htmlFor="valorDespesa">Valor (R$)</label>
        <input
          id="valorDespesa"
          className="input"
          type="number"
          min="0.01"
          step="0.01"
          required
          placeholder="0,00"
          style={{ borderRadius: 0 }}
          value={form.valor}
          onChange={(e) => onFormChange({ ...form, valor: e.target.value })}
        />
      </div>

      <div className="field w-[170px]">
        <label htmlFor="dataDespesa">Data</label>
        <input
          id="dataDespesa"
          className="input"
          type="date"
          required
          max={hojeInputDate()}
          style={{ borderRadius: 0 }}
          value={form.dataDespesa}
          onChange={(e) => onFormChange({ ...form, dataDespesa: e.target.value })}
        />
      </div>

      <div className="field min-w-[220px] flex-1">
        <label htmlFor="observacaoDespesa">Observação (opcional)</label>
        <input
          id="observacaoDespesa"
          className="input"
          type="text"
          maxLength={500}
          placeholder="Ex.: praça de pedágio da BR-116"
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
        {pending ? 'Salvando…' : editando ? 'Salvar alterações' : 'Lançar'}
      </button>

      <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
        O motorista é opcional: multa tem dono, IPVA e seguro não. O seletor de tipo mostra só os
        ativos — inative um tipo em <strong style={{ color: 'var(--color-text)' }}>Tipos de despesa</strong>{' '}
        para tirá-lo daqui sem apagar o histórico.
      </p>

      <div className="w-full">
        <ErrorList mensagens={erros} />
      </div>
    </InlineForm>
  )
}

function FiltrosDespesa({
  veiculos,
  tipos,
  motoristas,
  filtroVeiculo,
  filtroTipo,
  filtroMotorista,
  periodo,
  temFiltro,
  onFiltroVeiculoChange,
  onFiltroTipoChange,
  onFiltroMotoristaChange,
  onPeriodoChange,
  onLimpar,
}: {
  veiculos: VeiculoResponse[]
  tipos: TipoDespesaResponse[]
  motoristas: MotoristaResponse[]
  filtroVeiculo: string
  filtroTipo: string
  filtroMotorista: string
  periodo: Periodo
  temFiltro: boolean
  onFiltroVeiculoChange: (valor: string) => void
  onFiltroTipoChange: (valor: string) => void
  onFiltroMotoristaChange: (valor: string) => void
  onPeriodoChange: (valor: Periodo) => void
  onLimpar: () => void
}) {
  return (
    <div className="mb-5 flex flex-wrap items-end gap-4">
      <div className="field w-[220px]">
        <label htmlFor="filtroVeiculoDespesa">Veículo</label>
        <select
          id="filtroVeiculoDespesa"
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

      <div className="field w-[190px]">
        <label htmlFor="filtroTipoDespesa">Tipo</label>
        <select
          id="filtroTipoDespesa"
          className="input"
          style={{ borderRadius: 0 }}
          value={filtroTipo}
          onChange={(e) => onFiltroTipoChange(e.target.value)}
        >
          <option value="">Todos os tipos</option>
          {tipos.map((t) => (
            <option key={t.id} value={t.id}>
              {t.nome}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[210px]">
        <label htmlFor="filtroMotoristaDespesa">Motorista</label>
        <select
          id="filtroMotoristaDespesa"
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

      <FiltroPeriodo valor={periodo} onMudar={onPeriodoChange} id="filtroPeriodoDespesa" />

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

export function DespesasPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeLancar = pode.lancarDespesa(user?.role)
  // Exceção deliberada: aqui o Supervisor também exclui, não só o Admin.
  const podeExcluir = pode.excluirDespesa(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<DespesaResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])
  const [paraExcluir, setParaExcluir] = useState<DespesaResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  const [filtroVeiculo, setFiltroVeiculo] = useState('')
  const [filtroTipo, setFiltroTipo] = useState('')
  const [filtroMotorista, setFiltroMotorista] = useState('')
  const [periodo, setPeriodo] = useState<Periodo>('30dias')

  const intervalo = intervaloDoPeriodo(periodo)
  const filtro: DespesaFiltro = {
    veiculoId: filtroVeiculo === '' ? undefined : Number(filtroVeiculo),
    tipoDespesaId: filtroTipo === '' ? undefined : Number(filtroTipo),
    motoristaId: filtroMotorista === '' ? undefined : Number(filtroMotorista),
    de: intervalo.de,
    ate: intervalo.ate,
  }

  const despesasQuery = useQuery({
    queryKey: ['despesas', filtro],
    queryFn: () => despesasApi.getAll(filtro),
  })

  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })
  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })

  // Só os ativos: tipo aposentado não recebe lançamento novo (a API devolve 422).
  const tiposQuery = useQuery({
    queryKey: ['tiposDespesa', 'ativos'],
    queryFn: () => tiposDespesaApi.getAll(true),
  })

  /** O valor lançado aqui é uma das três origens que a tela de custos soma. */
  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ['despesas'] })
    queryClient.invalidateQueries({ queryKey: ['custos'] })
  }

  const salvarMutation = useMutation({
    mutationFn: () => {
      const corpo: DespesaRequest = {
        veiculoId: Number(form.veiculoId),
        tipoDespesaId: Number(form.tipoDespesaId),
        motoristaId: form.motoristaId === '' ? null : Number(form.motoristaId),
        valor: Number(form.valor),
        dataDespesa: form.dataDespesa,
        observacao: form.observacao.trim() === '' ? null : form.observacao.trim(),
      }
      // Diferente do abastecimento, a correção alcança todos os campos.
      return editando === null ? despesasApi.create(corpo) : despesasApi.update(editando.id, corpo)
    },
    onSuccess: () => {
      fecharForm()
      invalidar()
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível lançar a despesa.',
        ),
      ),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => despesasApi.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      invalidar()
    },
    onError: (error) => setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir a despesa.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    salvarMutation.mutate()
  }

  function abrirCadastro() {
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(despesa: DespesaResponse) {
    setEditando(despesa)
    setForm({
      veiculoId: String(despesa.veiculoId),
      tipoDespesaId: String(despesa.tipoDespesaId),
      motoristaId: despesa.motoristaId == null ? '' : String(despesa.motoristaId),
      valor: String(despesa.valor),
      dataDespesa: paraInputDate(despesa.dataDespesa),
      observacao: despesa.observacao ?? '',
    })
    setErros([])
    setAberto(true)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function fecharForm() {
    setAberto(false)
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
  }

  function limparFiltros() {
    setFiltroVeiculo('')
    setFiltroTipo('')
    setFiltroMotorista('')
    setPeriodo('todos')
  }

  const despesas = despesasQuery.data ?? []
  const tipos = tiposQuery.data ?? []
  const temFiltro = filtroVeiculo !== '' || filtroTipo !== '' || filtroMotorista !== '' || periodo !== 'todos'
  const mostrarAcoes = podeLancar || podeExcluir
  const colunas = mostrarAcoes ? 7 : 6

  /** Total do que está filtrado, não da frota inteira — como em `/abastecimentos`. */
  const total = useMemo(
    () => (despesasQuery.data ?? []).reduce((soma, d) => soma + d.valor, 0),
    [despesasQuery.data],
  )

  // Sem tipo cadastrado não há o que selecionar — o aviso evita o 422 sem explicação.
  const semTipos = tiposQuery.isSuccess && tipos.length === 0

  return (
    <AppLayout>
      <PageHeader
        titulo="Despesas"
        subtitulo="Custos avulsos da frota: pedágio, multa, IPVA, seguro, licenciamento. Entram na tela de custos junto com abastecimentos e manutenções."
        acoes={
          podeLancar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
              disabled={semTipos}
            >
              {aberto ? 'Cancelar' : 'Nova despesa'}
            </button>
          )
        }
      />

      {semTipos && (
        <p className="mb-4">
          <span className="tag tag-warning">
            Nenhum tipo de despesa ativo — cadastre um em "Tipos de despesa" antes de lançar.
          </span>
        </p>
      )}

      {aberto && podeLancar && (
        <DespesaFormulario
          editando={editando}
          form={form}
          onFormChange={setForm}
          onSubmit={handleSubmit}
          pending={salvarMutation.isPending}
          erros={erros}
          veiculos={veiculosQuery.data ?? []}
          tipos={tipos}
          motoristas={motoristasQuery.data ?? []}
        />
      )}

      <FiltrosDespesa
        veiculos={veiculosQuery.data ?? []}
        tipos={tipos}
        motoristas={motoristasQuery.data ?? []}
        filtroVeiculo={filtroVeiculo}
        filtroTipo={filtroTipo}
        filtroMotorista={filtroMotorista}
        periodo={periodo}
        temFiltro={temFiltro}
        onFiltroVeiculoChange={setFiltroVeiculo}
        onFiltroTipoChange={setFiltroTipo}
        onFiltroMotoristaChange={setFiltroMotorista}
        onPeriodoChange={setPeriodo}
        onLimpar={limparFiltros}
      />

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Veículo</th>
              <th>Tipo</th>
              <th>Motorista</th>
              <th>Valor</th>
              <th>Observação</th>
              {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={colunas}
              pending={despesasQuery.isPending}
              error={despesasQuery.error}
              empty={despesasQuery.isSuccess && despesas.length === 0}
              textoCarregando="Carregando despesas…"
              textoErro="Não foi possível carregar as despesas."
              textoVazio={
                temFiltro
                  ? 'Nenhuma despesa para os filtros escolhidos.'
                  : 'Nenhuma despesa lançada ainda.'
              }
            />
            {despesas.map((d) => (
              <tr key={d.id}>
                <td>{formatDate(d.dataDespesa)}</td>
                <td className="font-semibold">
                  {d.veiculoPlaca}
                  <div className="text-xs font-normal" style={{ color: mutedText }}>
                    {d.veiculoNome}
                  </div>
                </td>
                <td>{d.tipoDespesaNome}</td>
                <td>{d.motoristaNome ?? '—'}</td>
                <td className="font-semibold">{formatMoeda(d.valor)}</td>
                <td style={{ color: mutedText }}>{d.observacao || '—'}</td>
                {mostrarAcoes && (
                  <td>
                    <RowActions
                      descricao={`a despesa de ${formatMoeda(d.valor)}`}
                      onEditar={podeLancar ? () => abrirEdicao(d) : undefined}
                      onExcluir={
                        podeExcluir
                          ? () => {
                              setErrosExclusao([])
                              setParaExcluir(d)
                            }
                          : undefined
                      }
                    />
                  </td>
                )}
              </tr>
            ))}
          </tbody>
          {despesas.length > 0 && (
            <tfoot>
              <tr>
                <td colSpan={4} style={{ color: mutedText }}>
                  {despesas.length} {despesas.length === 1 ? 'lançamento' : 'lançamentos'}
                </td>
                <td className="font-semibold">
                  Total: <strong>{formatMoeda(total)}</strong>
                </td>
                <td colSpan={mostrarAcoes ? 2 : 1} />
              </tr>
            </tfoot>
          )}
        </table>
      </div>

      {paraExcluir && (
        <ConfirmDialog
          titulo="Excluir despesa"
          mensagem={`A despesa de ${formatMoeda(paraExcluir.valor)} (${paraExcluir.tipoDespesaNome}) do veículo ${paraExcluir.veiculoPlaca} será removida e sairá do total de custos.`}
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
