import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { motoristasApi } from '../api/motoristas'
import { mensagensDeErro } from '../api/errors'
import type { MotoristaRequest, MotoristaResponse } from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, InlineForm, RowActions, TableStates } from '../components/Table'
import { formatCpf, formatDate, mascaraCpf, paraInputDate, somenteDigitos } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = { nome: '', email: '', cpf: '', dataNascimento: '' }

export function MotoristasPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarCadastros(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<MotoristaResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])
  const [paraExcluir, setParaExcluir] = useState<MotoristaResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: MotoristaRequest }) =>
      id === null ? motoristasApi.create(body) : motoristasApi.update(id, body),
    onSuccess: () => {
      fecharForm()
      queryClient.invalidateQueries({ queryKey: ['motoristas'] })
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível cadastrar o motorista.',
        ),
      ),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => motoristasApi.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      queryClient.invalidateQueries({ queryKey: ['motoristas'] })
      // Uma rota mostra o nome do motorista: a lista de rotas fica desatualizada.
      queryClient.invalidateQueries({ queryKey: ['rotas'] })
    },
    onError: (error) => setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir o motorista.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    // A API guarda o CPF só com os 11 dígitos; a máscara é apenas visual.
    salvarMutation.mutate({ id: editando?.id ?? null, body: { ...form, cpf: somenteDigitos(form.cpf) } })
  }

  function abrirCadastro() {
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(motorista: MotoristaResponse) {
    setEditando(motorista)
    setForm({
      nome: motorista.nome,
      email: motorista.email,
      cpf: formatCpf(motorista.cpf),
      dataNascimento: paraInputDate(motorista.dataNascimento),
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

  const motoristas = motoristasQuery.data ?? []
  const mostrarAcoes = podeCadastrar || podeExcluir
  const colunas = mostrarAcoes ? 6 : 5

  return (
    <AppLayout>
      <PageHeader
        titulo="Motoristas"
        subtitulo="Cadastro de motoristas da frota."
        acoes={
          podeCadastrar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
            >
              {aberto ? 'Cancelar' : 'Novo motorista'}
            </button>
          )
        }
      />

      {aberto && podeCadastrar && (
        <InlineForm onSubmit={handleSubmit}>
          {editando && (
            <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
              Editando <strong style={{ color: 'var(--color-text)' }}>{editando.nome}</strong>.
            </p>
          )}
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
            disabled={salvarMutation.isPending}
          >
            {salvarMutation.isPending ? 'Salvando…' : editando ? 'Salvar alterações' : 'Cadastrar'}
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
              {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={colunas}
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
                {mostrarAcoes && (
                  <td>
                    <RowActions
                      descricao={`o motorista ${m.nome}`}
                      onEditar={podeCadastrar ? () => abrirEdicao(m) : undefined}
                      onExcluir={
                        podeExcluir
                          ? () => {
                              setErrosExclusao([])
                              setParaExcluir(m)
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
          titulo="Excluir motorista"
          mensagem={`${paraExcluir.nome} será removido da frota. Esta ação não pode ser desfeita.`}
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
