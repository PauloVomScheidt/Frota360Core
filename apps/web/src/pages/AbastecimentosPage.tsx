import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { abastecimentosApi } from '../api/abastecimentos'
import { tiposCombustivelApi } from '../api/tiposCombustivel'
import { postosApi } from '../api/postos'
import { motoristasApi } from '../api/motoristas'
import { rotasApi } from '../api/rotas'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import type {
  AbastecimentoFiltro,
  AbastecimentoResponse,
  MotoristaResponse,
  PostoResponse,
  RotaResponse,
  TipoCombustivelResponse,
  VeiculoResponse,
} from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, FiltroPeriodo, InlineForm, RowActions, TableStates } from '../components/Table'
import {
  formatConsumo,
  formatDate,
  formatKm,
  formatLitros,
  formatMoeda,
  hojeInputDate,
  paraInputDate,
} from '../lib/format'
import { intervaloDoPeriodo, type Periodo } from '../lib/periodo'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = {
  veiculoId: '',
  motoristaId: '',
  tipoCombustivelId: '',
  postoId: '',
  litros: '',
  valorLitro: '',
  odometro: '',
  notaFiscal: '',
  frentista: '',
  dataAbastecimento: '',
  observacao: '',
}

type FormularioAbastecimento = typeof FORM_VAZIO

/**
 * A média estimada desde o abastecimento anterior daquele veículo. Carrega a referência
 * junto de propósito: a tela nomeia a data e o odômetro dela para quem lança conseguir ver
 * que o cálculo pegou o lançamento certo — ou o errado.
 */
type ConsumoEstimado = {
  anterior: AbastecimentoResponse
  km: number
  kmPorLitro: number
}

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
  combustiveis,
  postos,
  motorista,
  nomeUsuario,
  rotaAtiva,
  valorTotal,
  consumoEstimado,
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
  combustiveis: TipoCombustivelResponse[]
  postos: PostoResponse[]
  motorista: boolean
  nomeUsuario: string
  rotaAtiva: RotaResponse | null
  /** Derivado de litros × valor do litro; o servidor recalcula e ignora o que chegar. */
  valorTotal: number
  /** Nulo quando não há abastecimento anterior daquele veículo abaixo do odômetro digitado. */
  consumoEstimado: ConsumoEstimado | null
}) {
  /**
   * Odômetro abaixo da ficha do veículo é **aceito** — pode ser lançamento retroativo, e o
   * servidor apenas não retrocede a quilometragem. Mas é também a cara de um erro de
   * digitação, e um número furado aqui envenena o km/l daquele veículo depois. O aviso pega
   * o dedo errado enquanto quem lança ainda sabe a verdade, sem bloquear o caso legítimo.
   */
  const quilometragemDaFicha = useMemo(() => {
    const veiculo = veiculos.find((v) => v.id === Number(form.veiculoId))
    const odometro = Number(form.odometro)

    if (!veiculo || form.odometro === '' || !Number.isFinite(odometro)) return null

    return odometro < veiculo.quilometragem ? veiculo.quilometragem : null
  }, [veiculos, form.veiculoId, form.odometro])

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

      <div className="field w-[190px]">
        <label htmlFor="tipoCombustivelId">Combustível</label>
        <select
          id="tipoCombustivelId"
          className="input"
          required
          style={{ borderRadius: 0 }}
          value={form.tipoCombustivelId}
          onChange={(e) => onFormChange({ ...form, tipoCombustivelId: e.target.value })}
        >
          <option value="">Selecione…</option>
          {combustiveis.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nome}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[220px]">
        <label htmlFor="postoId">Posto</label>
        <select
          id="postoId"
          className="input"
          required
          style={{ borderRadius: 0 }}
          value={form.postoId}
          onChange={(e) => onFormChange({ ...form, postoId: e.target.value })}
        >
          <option value="">Selecione…</option>
          {postos.map((p) => (
            <option key={p.id} value={p.id}>
              {p.nome}
              {p.cidade ? ` — ${p.cidade}` : ''}
            </option>
          ))}
        </select>
      </div>

      <div className="field w-[130px]">
        <label htmlFor="litros">Litros</label>
        <input
          id="litros"
          type="number"
          className="input"
          required
          min={0.001}
          step={0.001}
          placeholder="0,000"
          style={{ borderRadius: 0 }}
          value={form.litros}
          onChange={(e) => onFormChange({ ...form, litros: e.target.value })}
        />
      </div>

      <div className="field w-[140px]">
        <label htmlFor="valorLitro">Valor do litro (R$)</label>
        <input
          id="valorLitro"
          type="number"
          className="input"
          required
          min={0.001}
          step={0.001}
          placeholder="0,000"
          style={{ borderRadius: 0 }}
          value={form.valorLitro}
          onChange={(e) => onFormChange({ ...form, valorLitro: e.target.value })}
        />
      </div>

      {/* Somente leitura: o total é derivado, e quem o calcula de verdade é o servidor —
          este campo é o espelho do que ele vai gravar. */}
      <div className="field w-[150px]">
        <label htmlFor="valorTotal">Valor total (R$)</label>
        <input
          id="valorTotal"
          className="input"
          readOnly
          tabIndex={-1}
          style={{ borderRadius: 0, background: 'var(--color-surface)' }}
          value={formatMoeda(valorTotal)}
        />
      </div>

      <div className="field w-[150px]">
        <label htmlFor="odometro">Odômetro (km)</label>
        <input
          id="odometro"
          type="number"
          className="input"
          required
          min={1}
          step={1}
          placeholder="0"
          style={{ borderRadius: 0 }}
          value={form.odometro}
          onChange={(e) => onFormChange({ ...form, odometro: e.target.value })}
        />
      </div>

      <div className="field w-[160px]">
        <label htmlFor="notaFiscal">Nota fiscal</label>
        <input
          id="notaFiscal"
          className="input"
          required
          maxLength={30}
          style={{ borderRadius: 0 }}
          value={form.notaFiscal}
          onChange={(e) => onFormChange({ ...form, notaFiscal: e.target.value })}
        />
      </div>

      <div className="field w-[180px]">
        <label htmlFor="frentista">Frentista</label>
        <input
          id="frentista"
          className="input"
          maxLength={100}
          placeholder="Opcional"
          style={{ borderRadius: 0 }}
          value={form.frentista}
          onChange={(e) => onFormChange({ ...form, frentista: e.target.value })}
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

      {quilometragemDaFicha !== null && (
        <p className="m-0 w-full text-[13px]" style={{ color: 'var(--color-warning)' }}>
          ⚠ O odômetro informado é menor que a quilometragem atual do veículo (
          <strong>{formatKm(quilometragemDaFicha)}</strong>). Confirme se o lançamento é
          retroativo — a ficha do veículo não será alterada.
        </p>
      )}

      {consumoEstimado !== null && (
        <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
          Desde o abastecimento de{' '}
          <strong style={{ color: 'var(--color-text)' }}>
            {formatDate(consumoEstimado.anterior.dataAbastecimento)}
          </strong>{' '}
          ({formatKm(consumoEstimado.anterior.odometro)}): {formatKm(consumoEstimado.km)} ÷{' '}
          {formatLitros(Number(form.litros))} L ≈{' '}
          <strong style={{ color: 'var(--color-text)' }}>
            {formatConsumo(consumoEstimado.kmPorLitro)}
          </strong>{' '}
          — estimativa, e só fecha se os dois tanques foram enchidos por igual.
        </p>
      )}

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
              <th>Combustível</th>
              <th>Posto</th>
              <th>Abastecido</th>
              <th>Valor</th>
              <th>Observação</th>
              {mostrarAcoes && <th style={{ width: 90 }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={(motorista ? 8 : 9) + (mostrarAcoes ? 1 : 0)}
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
                <td>{a.tipoCombustivelNome}</td>
                <td>
                  <div>{a.postoNome}</div>
                  <div className="text-[12px]" style={{ color: mutedText }}>
                    NF {a.notaFiscal}
                  </div>
                </td>
                {/* Litros e preço na mesma célula: são um dado só, e a tabela já é larga. */}
                <td style={{ whiteSpace: 'nowrap' }}>
                  <div>{formatLitros(a.litros)} L</div>
                  <div className="text-[12px]" style={{ color: mutedText }}>
                    {formatMoeda(a.valorLitro)}/L · {a.odometro.toLocaleString('pt-BR')} km
                  </div>
                </td>
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

  // Os dois catálogos alimentam o formulário e a API os abre a todos os papéis — sem
  // `enabled`, ao contrário de `['motoristas']`. `apenasAtivos`: item aposentado continua
  // nomeando o passado mas não recebe lançamento novo.
  const combustiveisQuery = useQuery({
    queryKey: ['tiposCombustivel', 'ativos'],
    queryFn: () => tiposCombustivelApi.getAll(true),
  })

  const postosQuery = useQuery({
    queryKey: ['postos', 'ativos'],
    queryFn: () => postosApi.getAll(true),
  })

  /**
   * O histórico daquele veículo, **sem recorte de data** — o abastecimento anterior pode ser
   * de qualquer época, e o filtro da listagem (que abre no mês corrente) esconderia
   * justamente o que serve de referência. Só busca com o formulário aberto.
   *
   * A chave começa com `['abastecimentos']`, então o `invalidar()` já a alcança.
   */
  const historicoQuery = useQuery({
    queryKey: ['abastecimentos', 'doVeiculo', Number(form.veiculoId)],
    queryFn: () => abastecimentosApi.getAll({ veiculoId: Number(form.veiculoId) }),
    enabled: aberto && form.veiculoId !== '',
  })

  const abastecimentos = abastecimentosQuery.data ?? []
  const veiculos = veiculosQuery.data ?? []
  const motoristas = motoristasQuery.data ?? []
  const combustiveis = combustiveisQuery.data ?? []
  const postos = postosQuery.data ?? []
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

  /**
   * O abastecimento passou a mover o odômetro do veículo — é o **terceiro** caminho, ao
   * lado de abrir/encerrar rota e concluir manutenção. Por isso a cadeia inteira:
   * `atrasada`/`kmRestantes` da manutenção derivam do odômetro da ficha.
   *
   * `['custos']` continua entrando porque o valor é metade do que aquela tela soma.
   */
  function invalidar() {
    queryClient.invalidateQueries({ queryKey: ['abastecimentos'] })
    queryClient.invalidateQueries({ queryKey: ['custos'] })
    queryClient.invalidateQueries({ queryKey: ['veiculos'] })
    queryClient.invalidateQueries({ queryKey: ['manutencoes'] })
  }

  /**
   * Consumo desde o abastecimento anterior daquele veículo, no método tanque a tanque: os
   * litros que estão sendo colocados agora são os que repuseram o trecho percorrido.
   *
   * A referência é o abastecimento de **maior odômetro abaixo do digitado** — ordenar por
   * odômetro e não por data é o que impede um lançamento retroativo de virar km negativo.
   * Em modo de correção o próprio registro fica de fora.
   *
   * ⚠️ Para a role Motorista o número pode sair inflado: a lista dele vem recortada pelo
   * servidor, então se o abastecimento anterior daquele caminhão foi de outra pessoa a
   * referência será um lançamento mais antigo dele mesmo. É por isso que a tela nomeia a
   * data e o odômetro da referência em vez de mostrar só a média.
   */
  const consumoEstimado = useMemo<ConsumoEstimado | null>(() => {
    const odometro = Number(form.odometro)
    const litros = Number(form.litros)

    if (!Number.isFinite(odometro) || !Number.isFinite(litros) || litros <= 0) return null

    const anterior = (historicoQuery.data ?? [])
      .filter((a) => a.id !== editando?.id && a.odometro < odometro)
      .sort((a, b) => b.odometro - a.odometro)[0]

    if (anterior === undefined) return null

    const km = odometro - anterior.odometro

    return { anterior, km, kmPorLitro: km / litros }
  }, [historicoQuery.data, form.odometro, form.litros, editando])

  /**
   * O total é derivado, e o campo da tela é só um espelho: o corpo não o carrega e o
   * servidor o recalcula. Memoizado sobre os dois campos que o compõem.
   */
  const valorTotal = useMemo(() => {
    const litros = Number(form.litros)
    const preco = Number(form.valorLitro)
    if (!Number.isFinite(litros) || !Number.isFinite(preco)) return 0
    return Math.round(litros * preco * 100) / 100
  }, [form.litros, form.valorLitro])

  const salvarMutation = useMutation({
    mutationFn: () => {
      // `valor` fica de fora de propósito — quem o calcula é a API.
      const corpo = {
        tipoCombustivelId: Number(form.tipoCombustivelId),
        postoId: Number(form.postoId),
        litros: Number(form.litros),
        valorLitro: Number(form.valorLitro),
        odometro: Number(form.odometro),
        notaFiscal: form.notaFiscal.trim(),
        frentista: form.frentista.trim() === '' ? null : form.frentista.trim(),
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
      tipoCombustivelId: String(a.tipoCombustivelId),
      postoId: String(a.postoId),
      litros: String(a.litros),
      valorLitro: String(a.valorLitro),
      odometro: String(a.odometro),
      notaFiscal: a.notaFiscal,
      frentista: a.frentista ?? '',
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
  // Combustível e posto são obrigatórios: sem catálogo, o formulário não fecha.
  const semCombustiveis = combustiveisQuery.isSuccess && combustiveis.length === 0
  const semPostos = postosQuery.isSuccess && postos.length === 0
  const naoPodeAbrir = semVeiculos || semMotoristas || semCombustiveis || semPostos
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
            ? 'Seus abastecimentos. Registre o que abasteceu, onde e por quanto — o total sai de litros × valor do litro.'
            : 'Combustível da frota. Cada lançamento fica atribuído a um motorista e a um posto credenciado, para o gasto por pessoa, veículo, posto e período.'
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
                    : semCombustiveis
                      ? 'É preciso ter ao menos um tipo de combustível ativo no catálogo.'
                      : semPostos
                        ? 'É preciso ter ao menos um posto credenciado.'
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
          combustiveis={combustiveis}
          postos={postos}
          motorista={motorista}
          nomeUsuario={user?.nome ?? ''}
          rotaAtiva={rotaAtiva}
          valorTotal={valorTotal}
          consumoEstimado={consumoEstimado}
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
