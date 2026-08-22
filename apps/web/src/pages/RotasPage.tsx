import { useMemo, useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { rotasApi } from '../api/rotas'
import { motoristasApi } from '../api/motoristas'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import type { RotaResponse } from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { InlineForm, TableStates } from '../components/Table'
import { formatDate } from '../lib/format'

const FORM_VAZIO = { origem: '', destino: '', codigoMotorista: '', codigoVeiculo: '', dataInicio: '', dataFim: '' }

/**
 * A API não tem campo de status: derivamos de `ativo` + `dataFim`, que é o que
 * existe hoje (a RotaResponse é flat — ver §16 do CONTEXTO).
 */
function statusDaRota(rota: RotaResponse): { rotulo: string; classe: string } {
  if (rota.ativo) return { rotulo: 'Ativa', classe: 'tag tag-accent' }
  if (rota.dataFim) return { rotulo: 'Concluída', classe: 'tag tag-neutral' }
  return { rotulo: 'Inativa', classe: 'tag tag-neutral' }
}

export function RotasPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarRotas(user?.role)

  const [aberto, setAberto] = useState(false)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])

  const rotasQuery = useQuery({ queryKey: ['rotas'], queryFn: rotasApi.getAll })
  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })
  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })

  const motoristas = motoristasQuery.data ?? []
  const veiculos = veiculosQuery.data ?? []
  const rotas = rotasQuery.data ?? []

  // A resposta traz só os códigos das FKs — o cruzamento com nome/placa é aqui.
  const nomePorMotorista = useMemo(
    () => new Map((motoristasQuery.data ?? []).map((m) => [m.id, m.nome])),
    [motoristasQuery.data],
  )
  const placaPorVeiculo = useMemo(
    () => new Map((veiculosQuery.data ?? []).map((v) => [v.id, v.placa])),
    [veiculosQuery.data],
  )

  const criarMutation = useMutation({
    mutationFn: rotasApi.create,
    onSuccess: () => {
      setErros([])
      setForm(FORM_VAZIO)
      setAberto(false)
      queryClient.invalidateQueries({ queryKey: ['rotas'] })
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível cadastrar a rota.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    criarMutation.mutate({
      origem: form.origem,
      destino: form.destino,
      codigoMotorista: Number(form.codigoMotorista),
      codigoVeiculo: Number(form.codigoVeiculo),
      ativo: true,
      dataInicio: form.dataInicio,
      dataFim: form.dataFim || null,
    })
  }

  function alternar() {
    setAberto((v) => !v)
    setErros([])
  }

  const semCadastrosBase =
    (motoristasQuery.isSuccess && motoristas.length === 0) ||
    (veiculosQuery.isSuccess && veiculos.length === 0)

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
              onClick={alternar}
              disabled={semCadastrosBase && !aberto}
              title={semCadastrosBase ? 'Cadastre ao menos um motorista e um veículo antes.' : undefined}
            >
              {aberto ? 'Cancelar' : 'Nova rota'}
            </button>
          )
        }
      />

      {aberto && podeCadastrar && (
        <InlineForm onSubmit={handleSubmit}>
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
          <div className="field w-[200px]">
            <label htmlFor="codigoMotorista">Motorista</label>
            <select
              id="codigoMotorista"
              className="input"
              required
              style={{ borderRadius: 0 }}
              value={form.codigoMotorista}
              onChange={(e) => setForm({ ...form, codigoMotorista: e.target.value })}
            >
              <option value="">Selecione…</option>
              {motoristas.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.nome}
                </option>
              ))}
            </select>
          </div>
          <div className="field w-[200px]">
            <label htmlFor="codigoVeiculo">Veículo</label>
            <select
              id="codigoVeiculo"
              className="input"
              required
              style={{ borderRadius: 0 }}
              value={form.codigoVeiculo}
              onChange={(e) => setForm({ ...form, codigoVeiculo: e.target.value })}
            >
              <option value="">Selecione…</option>
              {veiculos.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.placa} — {v.nomeVeiculo}
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
              onChange={(e) => setForm({ ...form, dataInicio: e.target.value })}
            />
          </div>
          <div className="field w-[150px]">
            <label htmlFor="dataFim">Fim (opcional)</label>
            <input
              id="dataFim"
              className="input"
              type="date"
              style={{ borderRadius: 0 }}
              value={form.dataFim}
              onChange={(e) => setForm({ ...form, dataFim: e.target.value })}
            />
          </div>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ borderRadius: 0, padding: '10px 20px' }}
            disabled={criarMutation.isPending}
          >
            {criarMutation.isPending ? 'Cadastrando…' : 'Cadastrar'}
          </button>
          <div className="w-full">
            <ErrorList mensagens={erros} />
          </div>
        </InlineForm>
      )}

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Origem → Destino</th>
              <th>Motorista</th>
              <th>Veículo</th>
              <th>Início</th>
              <th>Fim</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={6}
              pending={rotasQuery.isPending}
              error={rotasQuery.error}
              empty={rotasQuery.isSuccess && rotas.length === 0}
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
                  <td>{nomePorMotorista.get(rota.codigoMotorista) ?? `#${rota.codigoMotorista}`}</td>
                  <td>{placaPorVeiculo.get(rota.codigoVeiculo) ?? `#${rota.codigoVeiculo}`}</td>
                  <td>{formatDate(rota.dataInicio)}</td>
                  <td>{formatDate(rota.dataFim)}</td>
                  <td>
                    <span className={status.classe}>{status.rotulo}</span>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </AppLayout>
  )
}
