import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { tiposCombustivelApi } from '../api/tiposCombustivel'
import { mensagensDeErro } from '../api/errors'
import type { TipoCombustivelResponse, TipoCombustivelUpdateRequest } from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, InlineForm, RowActions, TableStates } from '../components/Table'
import { formatDate } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = { nome: '', ativo: 'true' }

export function TiposCombustivelPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarTiposCombustivel(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<TipoCombustivelResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])
  const [erroLinha, setErroLinha] = useState<string[]>([])
  const [paraExcluir, setParaExcluir] = useState<TipoCombustivelResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  // Sem `apenasAtivos`: o catálogo precisa listar também os inativos para reativá-los.
  const tiposQuery = useQuery({
    queryKey: ['tiposCombustivel'],
    queryFn: () => tiposCombustivelApi.getAll(),
  })

  /**
   * O prefixo cobre o catálogo completo e a lista de ativos do seletor de lançamento.
   * `['abastecimentos']` entra porque o nome do combustível é desnormalizado na listagem —
   * renomear um tipo muda as duas telas.
   *
   * `['custos']` fica de fora de propósito: a categoria da linha de custo do abastecimento
   * é a constante "Combustível", não o nome do tipo.
   */
  function invalidarTipos() {
    queryClient.invalidateQueries({ queryKey: ['tiposCombustivel'] })
    queryClient.invalidateQueries({ queryKey: ['abastecimentos'] })
  }

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: TipoCombustivelUpdateRequest }) =>
      id === null ? tiposCombustivelApi.create({ nome: body.nome }) : tiposCombustivelApi.update(id, body),
    onSuccess: () => {
      fecharForm()
      invalidarTipos()
    },
    onError: (error) =>
      setErros(
        mensagensDeErro(
          error,
          editando ? 'Não foi possível salvar as alterações.' : 'Não foi possível cadastrar o tipo.',
        ),
      ),
  })

  // Atalho da linha: inativar tira o tipo do seletor sem apagar o histórico.
  const alternarAtivoMutation = useMutation({
    mutationFn: (tipo: TipoCombustivelResponse) =>
      tiposCombustivelApi.update(tipo.id, { nome: tipo.nome, ativo: !tipo.ativo }),
    onSuccess: () => {
      setErroLinha([])
      invalidarTipos()
    },
    onError: (error) => setErroLinha(mensagensDeErro(error, 'Não foi possível alterar a situação do tipo.')),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => tiposCombustivelApi.remove(id),
    onSuccess: (_, id) => {
      setParaExcluir(null)
      setErrosExclusao([])
      if (editando?.id === id) fecharForm()
      invalidarTipos()
    },
    // 422 aqui é o "tipo em uso": a própria API responde pedindo para inativar.
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

  function abrirEdicao(tipo: TipoCombustivelResponse) {
    setEditando(tipo)
    setForm({ nome: tipo.nome, ativo: String(tipo.ativo) })
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

  const tipos = tiposQuery.data ?? []
  const mostrarAcoes = podeCadastrar || podeExcluir
  const colunas = mostrarAcoes ? 4 : 3

  return (
    <AppLayout>
      <PageHeader
        titulo="Tipos de combustível"
        subtitulo="Catálogo da empresa — alimenta o seletor da tela de abastecimentos."
        acoes={
          podeCadastrar && (
            <button
              type="button"
              className="btn btn-primary"
              style={{ borderRadius: 0 }}
              onClick={aberto ? fecharForm : abrirCadastro}
            >
              {aberto ? 'Cancelar' : 'Novo tipo'}
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
            <label htmlFor="nomeTipoCombustivel">Nome</label>
            <input
              id="nomeTipoCombustivel"
              className="input"
              type="text"
              placeholder="Ex.: Diesel S10"
              maxLength={100}
              required
              style={{ borderRadius: 0 }}
              value={form.nome}
              onChange={(e) => setForm({ ...form, nome: e.target.value })}
            />
          </div>
          {editando && (
            <div className="field w-[150px]">
              <label htmlFor="ativoTipoCombustivel">Situação</label>
              <select
                id="ativoTipoCombustivel"
                className="input"
                style={{ borderRadius: 0 }}
                value={form.ativo}
                onChange={(e) => setForm({ ...form, ativo: e.target.value })}
              >
                <option value="true">Ativo</option>
                <option value="false">Inativo</option>
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
            O nome é único por empresa. Combustível inativo some do seletor de lançamento, mas
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
              <th>Tipo</th>
              <th>Situação</th>
              <th>Cadastrado em</th>
              {mostrarAcoes && <th style={{ textAlign: 'right' }}>Ações</th>}
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={colunas}
              pending={tiposQuery.isPending}
              error={tiposQuery.error}
              empty={tiposQuery.isSuccess && tipos.length === 0}
              textoCarregando="Carregando tipos…"
              textoErro="Não foi possível carregar os tipos de combustível."
              textoVazio="Nenhum tipo cadastrado ainda — cadastre o primeiro para poder lançar abastecimentos."
            />
            {tipos.map((tipo) => (
              <tr key={tipo.id} style={{ opacity: tipo.ativo ? 1 : 0.6 }}>
                <td className="font-semibold">{tipo.nome}</td>
                <td>
                  <span className={tipo.ativo ? 'tag tag-success' : 'tag tag-neutral'}>
                    {tipo.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td>{formatDate(tipo.dataInclusao)}</td>
                {mostrarAcoes && (
                  <td>
                    <div className="flex items-center justify-end gap-1">
                      {podeCadastrar && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          style={{ borderRadius: 0, padding: '6px 12px', fontSize: 12 }}
                          onClick={() => alternarAtivoMutation.mutate(tipo)}
                          disabled={alternarAtivoMutation.isPending}
                        >
                          {tipo.ativo ? 'Inativar' : 'Ativar'}
                        </button>
                      )}
                      <RowActions
                        descricao={`o tipo ${tipo.nome}`}
                        onEditar={podeCadastrar ? () => abrirEdicao(tipo) : undefined}
                        onExcluir={
                          podeExcluir
                            ? () => {
                                setErrosExclusao([])
                                setParaExcluir(tipo)
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
          titulo="Excluir tipo de combustível"
          mensagem={`"${paraExcluir.nome}" será removido do catálogo. Se já houver abastecimentos usando este combustível, a API recusa a exclusão — nesse caso, inative-o para tirá-lo do seletor sem apagar o histórico.`}
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
