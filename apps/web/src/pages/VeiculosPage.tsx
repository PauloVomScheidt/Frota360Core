import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { veiculosApi } from '../api/veiculos'
import { mensagensDeErro } from '../api/errors'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { InlineForm, TableStates } from '../components/Table'
import { formatDate, formatKm } from '../lib/format'

const FORM_VAZIO = { nomeVeiculo: '', marcaVeiculo: '', placa: '', quilometragem: '' }

export function VeiculosPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarCadastros(user?.role)

  const [aberto, setAberto] = useState(false)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])

  const veiculosQuery = useQuery({ queryKey: ['veiculos'], queryFn: veiculosApi.getAll })

  const criarMutation = useMutation({
    mutationFn: veiculosApi.create,
    onSuccess: () => {
      setErros([])
      setForm(FORM_VAZIO)
      setAberto(false)
      queryClient.invalidateQueries({ queryKey: ['veiculos'] })
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível cadastrar o veículo.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    criarMutation.mutate({
      nomeVeiculo: form.nomeVeiculo,
      marcaVeiculo: form.marcaVeiculo,
      placa: form.placa,
      quilometragem: Number(form.quilometragem) || 0,
    })
  }

  function alternar() {
    setAberto((v) => !v)
    setErros([])
  }

  const veiculos = veiculosQuery.data ?? []

  return (
    <AppLayout>
      <PageHeader
        titulo="Veículos"
        subtitulo="Cadastro de veículos da frota."
        acoes={
          podeCadastrar && (
            <button type="button" className="btn btn-primary" style={{ borderRadius: 0 }} onClick={alternar}>
              {aberto ? 'Cancelar' : 'Novo veículo'}
            </button>
          )
        }
      />

      {aberto && podeCadastrar && (
        <InlineForm onSubmit={handleSubmit}>
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
              <th>Veículo</th>
              <th>Marca</th>
              <th>Placa</th>
              <th>Quilometragem</th>
              <th>Último motorista</th>
              <th>Cadastrado em</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={6}
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
                <td>{formatKm(v.quilometragem)}</td>
                <td>{v.ultimoMotorista || '—'}</td>
                <td>{formatDate(v.dataInclusao)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AppLayout>
  )
}
