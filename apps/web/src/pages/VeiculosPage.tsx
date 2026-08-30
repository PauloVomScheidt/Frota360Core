import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import type { VeiculoRequest, VeiculoResponse } from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, InlineForm, RowActions, TableStates } from '../components/Table'
import { formatDate, formatKm } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = { nomeVeiculo: '', marcaVeiculo: '', placa: '', quilometragem: '' }

export function VeiculosPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarCadastros(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<VeiculoResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])
  const [paraExcluir, setParaExcluir] = useState<VeiculoResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: VeiculoRequest }) =>
      id === null ? veiculosApi.create(body) : veiculosApi.update(id, body),
    onSuccess: () => {
      fecharForm()
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível cadastrar o veículo.',
        ),
      ),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => veiculosApi.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
      // A lista de rotas mostra a placa do veículo.
      queryClient.invalidateQueries({ queryKey: ['rotas'] })
    },
    onError: (error) => setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir o veículo.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    salvarMutation.mutate({
      id: editando?.id ?? null,
      body: {
        nomeVeiculo: form.nomeVeiculo,
        marcaVeiculo: form.marcaVeiculo,
        placa: form.placa,
        quilometragem: Number(form.quilometragem) || 0,
        // O PUT substitui o registro inteiro: sem estes dois campos, o último
        // motorista e a última viagem (preenchidos pelas rotas) seriam apagados.
        ultimoMotorista: editando?.ultimoMotorista ?? null,
        dataUltimaViagem: editando?.dataUltimaViagem ?? null,
      },
    })
  }

  function abrirCadastro() {
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(veiculo: VeiculoResponse) {
    setEditando(veiculo)
    setForm({
      nomeVeiculo: veiculo.nomeVeiculo,
      marcaVeiculo: veiculo.marcaVeiculo,
      placa: veiculo.placa,
      quilometragem: String(veiculo.quilometragem),
    })
    setErros([])
    setAberto(true)
    // O formulário abre acima da tabela — a linha editada pode estar fora da tela.
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function fecharForm() {
    setAberto(false)
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
  }

  const veiculos = veiculosQuery.data ?? []
  const mostrarAcoes = podeCadastrar || podeExcluir
  const colunas = mostrarAcoes ? 8 : 7

  return (
    <AppLayout>
      <PageHeader
        titulo="Veículos"
        subtitulo="Cadastro de veículos da frota."
        acoes={
          podeCadastrar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
            >
              {aberto ? 'Cancelar' : 'Novo veículo'}
            </button>
          )
        }
      />

      {aberto && podeCadastrar && (
        <InlineForm onSubmit={handleSubmit}>
          {editando && (
            <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
              Editando <strong style={{ color: 'var(--color-text)' }}>{editando.placa}</strong> —{' '}
              {editando.nomeVeiculo}.
            </p>
          )}
          <div className="field min-w-[180px] flex-1">
            <label htmlFor="nomeVeiculo">Nome do veículo</label>
            <input
              id="nomeVeiculo"
              className="input"
              type="text"
              placeholder="Ex.: Caminhão Baú"
              required
              style={{ borderRadius: 0 }}
              value={form.nomeVeiculo}
              onChange={(e) => setForm({ ...form, nomeVeiculo: e.target.value })}
            />
          </div>
          <div className="field min-w-[160px] flex-1">
            <label htmlFor="marcaVeiculo">Marca</label>
            <input
              id="marcaVeiculo"
              className="input"
              type="text"
              placeholder="Ex.: Volvo"
              required
              style={{ borderRadius: 0 }}
              value={form.marcaVeiculo}
              onChange={(e) => setForm({ ...form, marcaVeiculo: e.target.value })}
            />
          </div>
          <div className="field w-[140px]">
            <label htmlFor="placa">Placa</label>
            <input
              id="placa"
              className="input"
              type="text"
              placeholder="FRT-0000"
              required
              style={{ borderRadius: 0 }}
              value={form.placa}
              onChange={(e) => setForm({ ...form, placa: e.target.value.toUpperCase() })}
            />
          </div>
          <div className="field w-[160px]">
            <label htmlFor="quilometragem">Quilometragem</label>
            <input
              id="quilometragem"
              className="input"
              type="number"
              min={0}
              placeholder="0"
              style={{ borderRadius: 0 }}
              value={form.quilometragem}
              onChange={(e) => setForm({ ...form, quilometragem: e.target.value })}
            />
          </div>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ borderRadius: 0, padding: '10px 20px' }}
            disabled={salvarMutation.isPending}
          >
            {salvarMutation.isPending ? 'Salvando…' : editando ? 'Salvar alterações' : 'Cadastrar'}
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
              <th>Veículo</th>
              <th>Marca</th>
              <th>Placa</th>
              <th>Situação</th>
              <th>Quilometragem</th>
              <th>Último motorista</th>
              <th>Cadastrado em</th>
              {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={colunas}
              pending={veiculosQuery.isPending}
              error={veiculosQuery.error}
              empty={veiculosQuery.isSuccess && veiculos.length === 0}
              textoCarregando="Carregando veículos…"
              textoErro="Não foi possível carregar os veículos."
              textoVazio="Nenhum veículo cadastrado ainda."
            />
            {veiculos.map((v) => (
              <tr key={v.id}>
                <td className="font-semibold">{v.nomeVeiculo}</td>
                <td>{v.marcaVeiculo}</td>
                <td>{v.placa}</td>
                <td>
                  {/* `emRota` vem derivado da API: a lista de rotas é fechada para o
                      motorista, que enxerga esta tela — cruzar aqui daria 403 para ele. */}
                  <span className={v.emRota ? 'tag tag-accent' : 'tag tag-success'}>
                    {v.emRota ? 'Em rota' : 'Disponível'}
                  </span>
                </td>
                <td>{formatKm(v.quilometragem)}</td>
                <td>{v.ultimoMotorista || '—'}</td>
                <td>{formatDate(v.dataInclusao)}</td>
                {mostrarAcoes && (
                  <td>
                    <RowActions
                      descricao={`o veículo ${v.placa}`}
                      onEditar={podeCadastrar ? () => abrirEdicao(v) : undefined}
                      onExcluir={
                        podeExcluir
                          ? () => {
                              setErrosExclusao([])
                              setParaExcluir(v)
                            }
                          : undefined
                      }
                    />
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {paraExcluir && (
        <ConfirmDialog
          titulo="Excluir veículo"
          mensagem={`${paraExcluir.placa} — ${paraExcluir.nomeVeiculo} será removido da frota. Esta ação não pode ser desfeita.`}
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
