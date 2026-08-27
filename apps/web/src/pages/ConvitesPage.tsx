import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { convitesApi } from '../api/convites'
import { mensagensDeErro } from '../api/errors'
import type { ConviteResponse, Role } from '../api/types'
import { DESCRICAO_ROLE, ROLES } from '../auth/permissions'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { InlineForm, TableStates } from '../components/Table'
import { formatDateTime } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

type StatusConvite = 'Utilizado' | 'Expirado' | 'Pendente'

function statusDoConvite(convite: ConviteResponse): StatusConvite {
  if (convite.utilizadoEm) return 'Utilizado'
  return new Date(convite.expiraEm).getTime() < Date.now() ? 'Expirado' : 'Pendente'
}

export function ConvitesPage() {
  const queryClient = useQueryClient()
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<Role>('Operador')
  const [linkGerado, setLinkGerado] = useState<string | null>(null)
  const [copiado, setCopiado] = useState(false)
  const [erros, setErros] = useState<string[]>([])

  const convitesQuery = useQuery({ queryKey: ['convites'], queryFn: convitesApi.getAll })
  const invalidar = () => queryClient.invalidateQueries({ queryKey: ['convites'] })

  const criarMutation = useMutation({
    mutationFn: convitesApi.criar,
    onSuccess: (convite) => {
      setErros([])
      setEmail('')
      setCopiado(false)
      // Em dev o e-mail só vai para o log da API — o link em claro é o caminho prático.
      setLinkGerado(convite.linkConvite ?? null)
      invalidar()
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível criar o convite.')),
  })

  const cancelarMutation = useMutation({
    mutationFn: convitesApi.cancelar,
    onSuccess: () => {
      setErros([])
      invalidar()
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível cancelar o convite.')),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    criarMutation.mutate({ email, role })
  }

  async function copiarLink() {
    if (!linkGerado) return
    await navigator.clipboard.writeText(linkGerado)
    setCopiado(true)
  }

  const convites = convitesQuery.data ?? []

  return (
    <AppLayout>
      <PageHeader
        titulo="Convites"
        subtitulo="Convide pessoas para a sua empresa. O convite vale 7 dias e só pode ser usado uma vez."
      />

      <InlineForm onSubmit={handleSubmit}>
        <div className="field min-w-[260px] flex-1">
          <label htmlFor="email">E-mail do convidado</label>
          <input
            id="email"
            className="input"
            type="email"
            placeholder="pessoa@empresa.com"
            required
            style={{ borderRadius: 0 }}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>
        <div className="field w-[180px]">
          <label htmlFor="role">Permissão</label>
          <select
            id="role"
            className="input"
            style={{ borderRadius: 0 }}
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
        <button
          type="submit"
          className="btn btn-primary"
          style={{ borderRadius: 0, padding: '10px 20px' }}
          disabled={criarMutation.isPending}
        >
          {criarMutation.isPending ? 'Enviando…' : 'Enviar convite'}
        </button>
        <p className="m-0 w-full text-xs" style={{ color: mutedText }}>
          {DESCRICAO_ROLE[role]} Reenviar para o mesmo e-mail invalida o convite pendente anterior.
        </p>
      </InlineForm>

      <div className="mb-4">
        <ErrorList mensagens={erros} />
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
            {convites.map((convite) => {
              const status = statusDoConvite(convite)
              return (
                <tr key={convite.id}>
                  <td className="font-semibold">{convite.email}</td>
                  <td>{convite.role}</td>
                  <td>
                    <span className={status === 'Pendente' ? 'tag tag-accent' : 'tag tag-neutral'}>{status}</span>
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
    </AppLayout>
  )
}
