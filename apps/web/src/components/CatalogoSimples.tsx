import { useState, type FormEvent, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { mensagensDeErro } from '../api/errors'
import type { Role } from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from './AppLayout'
import { ConfirmDialog, FormDialog, Paginacao, RowActions, SecaoCampos, TableStates } from './Table'
import { usePaginacao } from '../lib/paginacao'
import { formatDate } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = { nome: '', ativo: 'true' }

interface ItemCatalogo {
  id: number
  nome: string
  ativo: boolean
  dataInclusao: string
}

interface ApiCatalogo<T> {
  getAll: () => Promise<T[]>
  create: (body: { nome: string }) => Promise<T>
  update: (id: number, body: { nome: string; ativo: boolean }) => Promise<T>
  remove: (id: number) => Promise<void>
}

interface CatalogoSimplesProps<T> {
  api: ApiCatalogo<T>
  /** Raiz da chave de cache: cobre o catálogo completo e a lista de ativos do seletor. */
  chave: string
  /** Chaves das telas que desnormalizam o nome do item. */
  invalidarTambem: string[]
  podeEditar: (role?: Role) => boolean
  /** Sufixo dos ids dos campos do formulário (`nome<sufixo>`, `ativo<sufixo>`). */
  idCampos: string
  titulo: string
  subtitulo: string
  tituloNovo: string
  tituloEdicao: string
  placeholder: string
  nota: ReactNode
  textoErro: string
  textoVazio: string
  tituloExclusao: string
  mensagemExclusao: (nome: string) => string
}

/**
 * Tela de catálogo nome + situação (tipos de combustível e de despesa): cadastro e edição no
 * mesmo modal, inativar pela linha e excluir com confirmação.
 */
export function CatalogoSimples<T extends ItemCatalogo>({
  api,
  chave,
  invalidarTambem,
  podeEditar,
  idCampos,
  titulo,
  subtitulo,
  tituloNovo,
  tituloEdicao,
  placeholder,
  nota,
  textoErro,
  textoVazio,
  tituloExclusao,
  mensagemExclusao,
}: Readonly<CatalogoSimplesProps<T>>) {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = podeEditar(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<T | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])
  const [erroLinha, setErroLinha] = useState<string[]>([])
  const [paraExcluir, setParaExcluir] = useState<T | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  // Sem `apenasAtivos`: o catálogo precisa listar também os inativos para reativá-los.
  const itensQuery = useQuery({
    queryKey: [chave],
    queryFn: () => api.getAll(),
  })

  function invalidar() {
    for (const queryKey of [chave, ...invalidarTambem]) {
      queryClient.invalidateQueries({ queryKey: [queryKey] })
    }
  }

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: { nome: string; ativo: boolean } }) =>
      id === null ? api.create({ nome: body.nome }) : api.update(id, body),
    onSuccess: () => {
      fecharForm()
      invalidar()
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível cadastrar o tipo.',
        ),
      ),
  })

  // Atalho da linha: inativar tira o item do seletor sem apagar o histórico.
  const alternarAtivoMutation = useMutation({
    mutationFn: (item: T) => api.update(item.id, { nome: item.nome, ativo: !item.ativo }),
    onSuccess: () => {
      setErroLinha([])
      invalidar()
    },
    onError: (error) => setErroLinha(mensagensDeErro(error, 'Não foi possível alterar a situação do tipo.')),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => api.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      invalidar()
    },
    // 422 aqui é o "em uso": a própria API responde pedindo para inativar.
    onError: (error) => setErrosExclusao(mensagensDeErro(error, 'Não foi possível excluir o tipo.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    salvarMutation.mutate({
      id: editando?.id ?? null,
      body: { nome: form.nome.trim(), ativo: form.ativo === 'true' },
    })
  }

  function abrirCadastro() {
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(item: T) {
    setEditando(item)
    setForm({ nome: item.nome, ativo: String(item.ativo) })
    setErros([])
    setAberto(true)
  }

  function fecharForm() {
    setAberto(false)
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
  }

  const itens = itensQuery.data ?? []
  const p = usePaginacao(itens)
  const mostrarAcoes = podeCadastrar || podeExcluir
  const colunas = mostrarAcoes ? 4 : 3

  return (
    <AppLayout>
      <PageHeader
        titulo={titulo}
        subtitulo={subtitulo}
        acoes={
          podeCadastrar && (
            <button type="button" className="btn btn-primary" onClick={abrirCadastro}>
              Novo tipo
            </button>
          )
        }
      />

      {aberto && podeCadastrar && (
        <FormDialog
          titulo={editando ? tituloEdicao : tituloNovo}
          descricao={editando ? `Editando ${editando.nome}.` : undefined}
          textoConfirmar={editando ? 'Salvar alterações' : 'Cadastrar'}
          textoPendente="Salvando…"
          pending={salvarMutation.isPending}
          erros={erros}
          onSubmit={handleSubmit}
          onCancelar={fecharForm}
        >
          <SecaoCampos>
            <div className="field campo-largo">
              <label htmlFor={`nome${idCampos}`}>Nome</label>
              <input
                id={`nome${idCampos}`}
                className="input"
                type="text"
                placeholder={placeholder}
                maxLength={100}
                required
                value={form.nome}
                onChange={(e) => setForm({ ...form, nome: e.target.value })}
              />
            </div>
            {editando && (
              <div className="field">
                <label htmlFor={`ativo${idCampos}`}>Situação</label>
                <select
                  id={`ativo${idCampos}`}
                  className="input"
                  value={form.ativo}
                  onChange={(e) => setForm({ ...form, ativo: e.target.value })}
                >
                  <option value="true">Ativo</option>
                  <option value="false">Inativo</option>
                </select>
              </div>
            )}
            <p className="campo-largo m-0 text-[13px]" style={{ color: mutedText }}>
              {nota}
            </p>
          </SecaoCampos>
        </FormDialog>
      )}

      <div className="mb-3">
        <ErrorList mensagens={erroLinha} />
      </div>

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Tipo</th>
              <th>Situação</th>
              <th>Cadastrado em</th>
              {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={colunas}
              pending={itensQuery.isPending}
              error={itensQuery.error}
              empty={itensQuery.isSuccess && itens.length === 0}
              textoCarregando="Carregando tipos…"
              textoErro={textoErro}
              textoVazio={textoVazio}
            />
            {p.itensDaPagina.map((item) => (
              <tr key={item.id} style={{ opacity: item.ativo ? 1 : 0.6 }}>
                <td className="font-semibold">{item.nome}</td>
                <td>
                  <span className={item.ativo ? 'tag tag-success' : 'tag tag-neutral'}>
                    {item.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td>{formatDate(item.dataInclusao)}</td>
                {mostrarAcoes && (
                  <td>
                    <div className="flex items-center justify-end gap-1">
                      {podeCadastrar && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          style={{ padding: '6px 12px', fontSize: 12 }}
                          onClick={() => alternarAtivoMutation.mutate(item)}
                          disabled={alternarAtivoMutation.isPending}
                        >
                          {item.ativo ? 'Inativar' : 'Ativar'}
                        </button>
                      )}
                      <RowActions
                        descricao={`o tipo ${item.nome}`}
                        onEditar={podeCadastrar ? () => abrirEdicao(item) : undefined}
                        onExcluir={
                          podeExcluir
                            ? () => {
                                setErrosExclusao([])
                                setParaExcluir(item)
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

      <Paginacao {...p} pending={itensQuery.isFetching} />

      {paraExcluir && (
        <ConfirmDialog
          titulo={tituloExclusao}
          mensagem={mensagemExclusao(paraExcluir.nome)}
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
