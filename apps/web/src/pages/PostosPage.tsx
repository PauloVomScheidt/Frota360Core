import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { postosApi } from '../api/postos'
import { mensagensDeErro } from '../api/errors'
import type { PostoResponse, PostoUpdateRequest } from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, InlineForm, RowActions, TableStates } from '../components/Table'
import { formatDate } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = { nome: '', cnpj: '', cidade: '', ativo: 'true' }

export function PostosPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarPostos(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<PostoResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])
  const [erroLinha, setErroLinha] = useState<string[]>([])
  const [paraExcluir, setParaExcluir] = useState<PostoResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  // Sem `apenasAtivos`: o catálogo precisa listar também os inativos para reativá-los.
  const postosQuery = useQuery({
    queryKey: ['postos'],
    queryFn: () => postosApi.getAll(),
  })

  /**
   * `['abastecimentos']` entra porque o nome do posto é desnormalizado na listagem —
   * renomear um posto muda as duas telas. `['custos']` fica de fora: a categoria da linha
   * de custo do abastecimento é a constante "Combustível", não o posto.
   */
  function invalidarPostos() {
    queryClient.invalidateQueries({ queryKey: ['postos'] })
    queryClient.invalidateQueries({ queryKey: ['abastecimentos'] })
  }

  function corpoDoForm(): PostoUpdateRequest {
    return {
      nome: form.nome.trim(),
      cnpj: form.cnpj.trim() === '' ? null : form.cnpj.trim(),
      cidade: form.cidade.trim() === '' ? null : form.cidade.trim(),
      ativo: form.ativo === 'true',
    }
  }

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: PostoUpdateRequest }) =>
      id === null
        ? postosApi.create({ nome: body.nome, cnpj: body.cnpj, cidade: body.cidade })
        : postosApi.update(id, body),
    onSuccess: () => {
      fecharForm()
      invalidarPostos()
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível cadastrar o posto.',
        ),
      ),
  })

  // Atalho da linha: descredenciar tira o posto do seletor sem apagar o histórico.
  const alternarAtivoMutation = useMutation({
    mutationFn: (posto: PostoResponse) =>
      postosApi.update(posto.id, {
        nome: posto.nome,
        cnpj: posto.cnpj,
        cidade: posto.cidade,
        ativo: !posto.ativo,
      }),
    onSuccess: () => {
      setErroLinha([])
      invalidarPostos()
    },
    onError: (error) => setErroLinha(mensagensDeErro(error, 'Não foi possível alterar a situação do posto.')),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => postosApi.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      invalidarPostos()
    },
    // 422 aqui é o "posto em uso": a própria API responde pedindo para inativar.
    onError: (error) => setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir o posto.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    salvarMutation.mutate({ id: editando?.id ?? null, body: corpoDoForm() })
  }

  function abrirCadastro() {
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(posto: PostoResponse) {
    setEditando(posto)
    setForm({
      nome: posto.nome,
      cnpj: posto.cnpj ?? '',
      cidade: posto.cidade ?? '',
      ativo: String(posto.ativo),
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

  const postos = postosQuery.data ?? []
  const mostrarAcoes = podeCadastrar || podeExcluir
  const colunas = mostrarAcoes ? 6 : 5

  return (
    <AppLayout>
      <PageHeader
        titulo="Postos"
        subtitulo="Rede credenciada da empresa — alimenta o seletor da tela de abastecimentos."
        acoes={
          podeCadastrar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
            >
              {aberto ? 'Cancelar' : 'Novo posto'}
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
          <div className="field min-w-[220px] flex-1">
            <label htmlFor="nomePosto">Nome</label>
            <input
              id="nomePosto"
              className="input"
              type="text"
              placeholder="Ex.: Posto Ipiranga BR-101"
              maxLength={100}
              required
              style={{ borderRadius: 0 }}
              value={form.nome}
              onChange={(e) => setForm({ ...form, nome: e.target.value })}
            />
          </div>
          <div className="field w-[190px]">
            <label htmlFor="cnpjPosto">CNPJ</label>
            <input
              id="cnpjPosto"
              className="input"
              type="text"
              placeholder="Opcional"
              maxLength={18}
              style={{ borderRadius: 0 }}
              value={form.cnpj}
              onChange={(e) => setForm({ ...form, cnpj: e.target.value })}
            />
          </div>
          <div className="field w-[190px]">
            <label htmlFor="cidadePosto">Cidade</label>
            <input
              id="cidadePosto"
              className="input"
              type="text"
              placeholder="Opcional"
              maxLength={100}
              style={{ borderRadius: 0 }}
              value={form.cidade}
              onChange={(e) => setForm({ ...form, cidade: e.target.value })}
            />
          </div>
          {editando && (
            <div className="field w-[150px]">
              <label htmlFor="ativoPosto">Situação</label>
              <select
                id="ativoPosto"
                className="input"
                style={{ borderRadius: 0 }}
                value={form.ativo}
                onChange={(e) => setForm({ ...form, ativo: e.target.value })}
              >
                <option value="true">Credenciado</option>
                <option value="false">Descredenciado</option>
              </select>
            </div>
          )}
          <button
            type="submit"
            className="btn btn-primary"
            style={{ borderRadius: 0, padding: '10px 20px' }}
            disabled={salvarMutation.isPending}
          >
            {salvarMutation.isPending ? 'Salvando…' : editando ? 'Salvar alterações' : 'Cadastrar'}
          </button>
          <p className="m-0 w-full text-[13px]" style={{ color: mutedText }}>
            O nome é único por empresa. Posto descredenciado some do seletor de lançamento, mas
            continua nomeando os abastecimentos antigos.
          </p>
          <div className="w-full">
            <ErrorList mensagens={erros} />
          </div>
        </InlineForm>
      )}

      <div className="mb-3">
        <ErrorList mensagens={erroLinha} />
      </div>

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Posto</th>
              <th>CNPJ</th>
              <th>Cidade</th>
              <th>Situação</th>
              <th>Cadastrado em</th>
              {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={colunas}
              pending={postosQuery.isPending}
              error={postosQuery.error}
              empty={postosQuery.isSuccess && postos.length === 0}
              textoCarregando="Carregando postos…"
              textoErro="Não foi possível carregar os postos."
              textoVazio="Nenhum posto credenciado ainda — cadastre o primeiro para poder lançar abastecimentos."
            />
            {postos.map((posto) => (
              <tr key={posto.id} style={{ opacity: posto.ativo ? 1 : 0.6 }}>
                <td className="font-semibold">{posto.nome}</td>
                <td>{posto.cnpj ?? '—'}</td>
                <td>{posto.cidade ?? '—'}</td>
                <td>
                  <span className={posto.ativo ? 'tag tag-success' : 'tag tag-neutral'}>
                    {posto.ativo ? 'Credenciado' : 'Descredenciado'}
                  </span>
                </td>
                <td>{formatDate(posto.dataInclusao)}</td>
                {mostrarAcoes && (
                  <td>
                    <div className="flex items-center justify-end gap-1">
                      {podeCadastrar && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          style={{ borderRadius: 0, padding: '6px 12px', fontSize: 12 }}
                          onClick={() => alternarAtivoMutation.mutate(posto)}
                          disabled={alternarAtivoMutation.isPending}
                        >
                          {posto.ativo ? 'Descredenciar' : 'Credenciar'}
                        </button>
                      )}
                      <RowActions
                        descricao={`o posto ${posto.nome}`}
                        onEditar={podeCadastrar ? () => abrirEdicao(posto) : undefined}
                        onExcluir={
                          podeExcluir
                            ? () => {
                                setErrosExclusao([])
                                setParaExcluir(posto)
                              }
                            : undefined
                        }
                      />
                    </div>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {paraExcluir && (
        <ConfirmDialog
          titulo="Excluir posto"
          mensagem={`"${paraExcluir.nome}" será removido do catálogo. Se já houver abastecimentos lançados neste posto, a API recusa a exclusão — nesse caso, descredencie-o para tirá-lo do seletor sem apagar o histórico.`}
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
