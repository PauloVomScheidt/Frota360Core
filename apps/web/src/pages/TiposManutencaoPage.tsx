import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { tiposManutencaoApi } from '../api/tiposManutencao'
import { mensagensDeErro } from '../api/errors'
import type { TipoManutencaoResponse, TipoManutencaoUpdateRequest } from '../api/types'
import { pode } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, InlineForm, RowActions, TableStates } from '../components/Table'
import { formatDate, formatKm } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const FORM_VAZIO = { nome: '', intervaloKm: '', ativo: 'true' }

export function TiposManutencaoPage() {
  const queryClient = useQueryClient()
  const user = useSession()
  const podeCadastrar = pode.editarTiposManutencao(user?.role)
  const podeExcluir = pode.excluir(user?.role)

  const [aberto, setAberto] = useState(false)
  const [editando, setEditando] = useState<TipoManutencaoResponse | null>(null)
  const [form, setForm] = useState(FORM_VAZIO)
  const [erros, setErros] = useState<string[]>([])
  const [erroLinha, setErroLinha] = useState<string[]>([])
  const [paraExcluir, setParaExcluir] = useState<TipoManutencaoResponse | null>(null)
  const [errosExclusao, setErrosExclusao] = useState<string[]>([])

  // Sem `apenasAtivos`: o catálogo precisa listar também os inativos para reativá-los.
  const tiposQuery = useQuery({
    queryKey: ['tiposManutencao'],
    queryFn: () => tiposManutencaoApi.getAll(),
  })

  // Invalida o catálogo completo e a lista de ativos (['tiposManutencao', 'ativos'])
  // usada pelo seletor de agendamento — o prefixo cobre as duas.
  function invalidarTipos() {
    queryClient.invalidateQueries({ queryKey: ['tiposManutencao'] })
  }

  // Cadastro e edição compartilham o mesmo formulário: o id decide o verbo HTTP.
  const salvarMutation = useMutation({
    mutationFn: ({ id, body }: { id: number | null; body: TipoManutencaoUpdateRequest }) =>
      id === null
        ? tiposManutencaoApi.create({ nome: body.nome, intervaloKm: body.intervaloKm })
        : tiposManutencaoApi.update(id, body),
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
    mutationFn: (tipo: TipoManutencaoResponse) =>
      tiposManutencaoApi.update(tipo.id, {
        nome: tipo.nome,
        intervaloKm: tipo.intervaloKm ?? null,
        ativo: !tipo.ativo,
      }),
    onSuccess: () => {
      setErroLinha([])
      invalidarTipos()
    },
    onError: (error) => setErroLinha(mensagensDeErro(error, 'Não foi possível alterar a situação do tipo.')),
  })

  const excluirMutation = useMutation({
    mutationFn: (id: number) => tiposManutencaoApi.remove(id),
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
      body: {
        nome: form.nome.trim(),
        // Opcional: em branco vira null, não 0 (a API exige > 0 quando informado).
        intervaloKm: form.intervaloKm === '' ? null : Number(form.intervaloKm),
        ativo: form.ativo === 'true',
      },
    })
  }

  function abrirCadastro() {
    setEditando(null)
    setForm(FORM_VAZIO)
    setErros([])
    setAberto(true)
  }

  function abrirEdicao(tipo: TipoManutencaoResponse) {
    setEditando(tipo)
    setForm({
      nome: tipo.nome,
      intervaloKm: tipo.intervaloKm == null ? '' : String(tipo.intervaloKm),
      ativo: String(tipo.ativo),
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

  const tipos = tiposQuery.data ?? []
  const mostrarAcoes = podeCadastrar || podeExcluir
  const colunas = mostrarAcoes ? 5 : 4

  return (
    <AppLayout>
      <PageHeader
        titulo="Tipos de manutenção"
        subtitulo="Catálogo da empresa — alimenta o seletor da tela de manutenções."
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
            <label htmlFor="nome">Nome</label>
            <input
              id="nome"
              className="input"
              type="text"
              placeholder="Ex.: Troca de óleo"
              maxLength={100}
              required
              style={{ borderRadius: 0 }}
              value={form.nome}
              onChange={(e) => setForm({ ...form, nome: e.target.value })}
            />
          </div>
          <div className="field w-[190px]">
            <label htmlFor="intervaloKm">Intervalo (km, opcional)</label>
            <input
              id="intervaloKm"
              className="input"
              type="number"
              min={1}
              placeholder="Ex.: 10000"
              style={{ borderRadius: 0 }}
              value={form.intervaloKm}
              onChange={(e) => setForm({ ...form, intervaloKm: e.target.value })}
            />
          </div>
          {editando && (
            <div className="field w-[150px]">
              <label htmlFor="ativo">Situação</label>
              <select
                id="ativo"
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
            O nome é único por empresa. O intervalo é informativo: serve para sugerir a quilometragem no
            agendamento — a manutenção seguinte ainda não é gerada automaticamente.
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
              <th>Intervalo</th>
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
              textoErro="Não foi possível carregar os tipos de manutenção."
              textoVazio="Nenhum tipo cadastrado ainda — cadastre o primeiro para poder agendar manutenções."
            />
            {tipos.map((tipo) => (
              <tr key={tipo.id} style={{ opacity: tipo.ativo ? 1 : 0.6 }}>
                <td className="font-semibold">{tipo.nome}</td>
                <td>{tipo.intervaloKm ? formatKm(tipo.intervaloKm) : '—'}</td>
                <td>
                  <span className={tipo.ativo ? 'tag tag-accent' : 'tag tag-neutral'}>
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
          titulo="Excluir tipo de manutenção"
          mensagem={`"${paraExcluir.nome}" será removido do catálogo. Se já houver manutenções usando este tipo, a API recusa a exclusão — nesse caso, inative-o para tirá-lo do seletor sem apagar o histórico.`}
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
