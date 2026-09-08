import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { convitesApi } from '../api/convites'
import { mensagensDeErro } from '../api/errors'
import type { ConviteResponse, Role } from '../api/types'
import { DESCRICAO_ROLE, ROLES } from '../auth/permissions'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { FormDialog, Paginacao, SecaoCampos, TableStates } from '../components/Table'
import { usePaginacao } from '../lib/paginacao'
import { formatDateTime } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

type StatusConvite = 'Utilizado' | 'Expirado' | 'Pendente'

function statusDoConvite(convite: ConviteResponse): StatusConvite {
  if (convite.utilizadoEm) return 'Utilizado'
  return new Date(convite.expiraEm).getTime() < Date.now() ? 'Expirado' : 'Pendente'
}

/**
 * Os três estados têm cores próprias: um convite expirado falhou e pede reenvio, e
 * antes ficava idêntico a um utilizado — os dois eram neutros.
 */
const CLASSE_STATUS: Record<StatusConvite, string> = {
  Pendente: 'tag tag-accent',
  Utilizado: 'tag tag-success',
  Expirado: 'tag tag-warning',
}

export function ConvitesPage() {
  const queryClient = useQueryClient()
  const [aberto, setAberto] = useState(false)
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<Role>('Operador')
  const [linkGerado, setLinkGerado] = useState<string | null>(null)
  const [copiado, setCopiado] = useState(false)
  // Dois destinos diferentes: o erro do envio vive dentro do modal, o do cancelamento
  // de linha fica na página — o modal nem está aberto quando ele acontece.
  const [erros, setErros] = useState<string[]>([])
  const [erroLinha, setErroLinha] = useState<string[]>([])

  const convitesQuery = useQuery({ queryKey: ['convites'], queryFn: convitesApi.getAll })
  const invalidar = () => queryClient.invalidateQueries({ queryKey: ['convites'] })

  const criarMutation = useMutation({
    mutationFn: convitesApi.criar,
    onSuccess: (convite) => {
      setErros([])
      setEmail('')
      setCopiado(false)
      setAberto(false)
      // Em dev o e-mail só vai para o log da API — o link em claro é o caminho prático.
      setLinkGerado(convite.linkConvite ?? null)
      invalidar()
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível criar o convite.')),
  })

  const cancelarMutation = useMutation({
    mutationFn: convitesApi.cancelar,
    onSuccess: () => {
      setErroLinha([])
      invalidar()
    },
    onError: (error) => setErroLinha(mensagensDeErro(error, 'Não foi possível cancelar o convite.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    criarMutation.mutate({ email, role })
  }

  function abrirCadastro() {
    setEmail('')
    setRole('Operador')
    setErros([])
    setAberto(true)
  }

  function fecharForm() {
    setAberto(false)
    setErros([])
  }

  async function copiarLink() {
    if (!linkGerado) return
    await navigator.clipboard.writeText(linkGerado)
    setCopiado(true)
  }

  const convites = convitesQuery.data ?? []
  const p = usePaginacao(convites)

  return (
    <AppLayout>
      <PageHeader
        titulo="Convites"
        subtitulo="Convide pessoas para a sua empresa. O convite vale 7 dias e só pode ser usado uma vez."
        acoes={
          <button type="button" className="btn btn-primary" onClick={abrirCadastro}>
            Novo convite
          </button>
        }
      />

      {aberto && (
        <FormDialog
          titulo="Novo convite"
          textoConfirmar="Enviar convite"
          textoPendente="Enviando…"
          pending={criarMutation.isPending}
          erros={erros}
          onSubmit={handleSubmit}
          onCancelar={fecharForm}
        >
          <SecaoCampos>
            <div className="field campo-largo">
              <label htmlFor="email">E-mail do convidado</label>
              <input
                id="email"
                className="input"
                type="email"
                placeholder="pessoa@empresa.com"
                required
                autoFocus
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>
            <div className="field">
              <label htmlFor="role">Permissão</label>
              <select
                id="role"
                className="input"
                value={role}
                onChange={(e) => setRole(e.target.value as Role)}
              >
                {ROLES.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </div>
            <p className="campo-largo m-0 text-xs" style={{ color: mutedText }}>
              {DESCRICAO_ROLE[role]} Reenviar para o mesmo e-mail invalida o convite pendente
              anterior.
            </p>
          </SecaoCampos>
        </FormDialog>
      )}

      <div className="mb-4">
        <ErrorList mensagens={erroLinha} />
      </div>

      {linkGerado && (
        <div
          className="mb-8 flex flex-wrap items-center gap-3 p-4"
          style={{ border: '1px solid var(--color-accent-300)', background: 'var(--color-accent-100)' }}
        >
          <div className="min-w-[240px] flex-1">
            <div
              className="mb-1 text-[11px] uppercase"
              style={{ letterSpacing: '0.08em', color: 'var(--color-accent-800)' }}
            >
              Link do convite
            </div>
            <code className="text-xs break-all" style={{ color: 'var(--color-accent-800)' }}>
              {linkGerado}
            </code>
          </div>
          <button type="button" className="btn btn-secondary" style={{ borderRadius: 0 }} onClick={copiarLink}>
            {copiado ? 'Copiado!' : 'Copiar link'}
          </button>
        </div>
      )}

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>E-mail</th>
              <th>Permissão</th>
              <th>Status</th>
              <th>Expira em</th>
              <th>Criado em</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={6}
              pending={convitesQuery.isPending}
              error={convitesQuery.error}
              empty={convitesQuery.isSuccess && convites.length === 0}
              textoCarregando="Carregando convites…"
              textoErro="Não foi possível carregar os convites."
              textoVazio="Nenhum convite enviado ainda."
            />
            {p.itensDaPagina.map((convite) => {
              const status = statusDoConvite(convite)
              return (
                <tr key={convite.id}>
                  <td className="font-semibold">{convite.email}</td>
                  <td>{convite.role}</td>
                  <td>
                    <span className={CLASSE_STATUS[status]}>{status}</span>
                  </td>
                  <td>{formatDateTime(convite.expiraEm)}</td>
                  <td>{formatDateTime(convite.dataInclusao)}</td>
                  <td>
                    {status === 'Utilizado' ? (
                      <span className="text-xs" style={{ color: mutedText }}>
                        Aceito em {formatDateTime(convite.utilizadoEm)}
                      </span>
                    ) : (
                      <button
                        type="button"
                        className="btn btn-secondary"
                        style={{ borderRadius: 0 }}
                        disabled={cancelarMutation.isPending}
                        onClick={() => cancelarMutation.mutate(convite.id)}
                      >
                        Cancelar
                      </button>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <Paginacao {...p} pending={convitesQuery.isFetching} />
    </AppLayout>
  )
}
