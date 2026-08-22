import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { motoristasApi } from '../api/motoristas'
import { mensagensDeErro } from '../api/errors'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { InlineForm, TableStates } from '../components/Table'
import { formatCpf, formatDate, mascaraCpf, somenteDigitos } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = { nome: '', email: '', cpf: '', dataNascimento: '' }

export function MotoristasPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarCadastros(user?.role)

  const [aberto, setAberto] = useState(false)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])

  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })

  const criarMutation = useMutation({
    mutationFn: motoristasApi.create,
    onSuccess: () => {
      setErros([])
      setForm(FORM_VAZIO)
      setAberto(false)
      queryClient.invalidateQueries({ queryKey: ['motoristas'] })
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível cadastrar o motorista.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    // A API guarda o CPF só com os 11 dígitos; a máscara é apenas visual.
    criarMutation.mutate({ ...form, cpf: somenteDigitos(form.cpf) })
  }

  function fechar() {
    setAberto((v) => !v)
    setErros([])
  }

  const motoristas = motoristasQuery.data ?? []

  return (
    <AppLayout>
      <PageHeader
        titulo="Motoristas"
        subtitulo="Cadastro de motoristas da frota."
        acoes={
          podeCadastrar && (
            <button type="button" className="btn btn-primary" style={{ borderRadius: 0 }} onClick={fechar}>
              {aberto ? 'Cancelar' : 'Novo motorista'}
            </button>
          )
        }
      />

      {aberto && podeCadastrar && (
        <InlineForm onSubmit={handleSubmit}>
          <div className="field min-w-[200px] flex-1">
            <label htmlFor="nome">Nome completo</label>
            <input
              id="nome"
              className="input"
              type="text"
              placeholder="Nome do motorista"
              required
              style={{ borderRadius: 0 }}
              value={form.nome}
              onChange={(e) => setForm({ ...form, nome: e.target.value })}
            />
          </div>
          <div className="field min-w-[200px] flex-1">
            <label htmlFor="email">E-mail</label>
            <input
              id="email"
              className="input"
              type="email"
              placeholder="motorista@empresa.com"
              required
              style={{ borderRadius: 0 }}
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />
          </div>
          <div className="field w-[160px]">
            <label htmlFor="cpf">CPF</label>
            <input
              id="cpf"
              className="input"
              type="text"
              inputMode="numeric"
              placeholder="000.000.000-00"
              required
              style={{ borderRadius: 0 }}
              value={form.cpf}
              onChange={(e) => setForm({ ...form, cpf: mascaraCpf(e.target.value) })}
            />
          </div>
          <div className="field w-[160px]">
            <label htmlFor="dataNascimento">Data de nascimento</label>
            <input
              id="dataNascimento"
              className="input"
              type="date"
              required
              style={{ borderRadius: 0 }}
              value={form.dataNascimento}
              onChange={(e) => setForm({ ...form, dataNascimento: e.target.value })}
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
          <p className="m-0 w-full text-xs" style={{ color: mutedText }}>
            Motorista precisa ter 18 anos ou mais. E-mail e CPF são únicos por empresa.
          </p>
          <div className="w-full">
            <ErrorList mensagens={erros} />
          </div>
        </InlineForm>
      )}

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Nome</th>
              <th>E-mail</th>
              <th>CPF</th>
              <th>Nascimento</th>
              <th>Cadastrado em</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={5}
              pending={motoristasQuery.isPending}
              error={motoristasQuery.error}
              empty={motoristasQuery.isSuccess && motoristas.length === 0}
              textoCarregando="Carregando motoristas…"
              textoErro="Não foi possível carregar os motoristas."
              textoVazio="Nenhum motorista cadastrado ainda."
            />
            {motoristas.map((m) => (
              <tr key={m.id}>
                <td className="font-semibold">{m.nome}</td>
                <td>{m.email}</td>
                <td>{formatCpf(m.cpf)}</td>
                <td>{formatDate(m.dataNascimento)}</td>
                <td>{formatDate(m.dataInclusao)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </AppLayout>
  )
}
