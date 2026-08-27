import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usuariosApi } from '../api/usuarios'
import { mensagensDeErro } from '../api/errors'
import type { Role, UsuarioResponse } from '../api/types'
import { ROLES } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { TableStates } from '../components/Table'
import { formatDate } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

export function UsuariosPage() {
  const queryClient = useQueryClient()
  const sessao = useSession()
  const [erros, setErros] = useState<string[]>([])

  const usuariosQuery = useQuery({ queryKey: ['usuarios'], queryFn: usuariosApi.getAll })

  const invalidar = () => queryClient.invalidateQueries({ queryKey: ['usuarios'] })

  const roleMutation = useMutation({
    mutationFn: ({ id, role }: { id: number; role: Role }) => usuariosApi.alterarRole(id, role),
    onSuccess: () => {
      setErros([])
      invalidar()
    },
    // Ex.: rebaixar o último admin ativo → 422 com a mensagem da regra.
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível alterar a permissão.')),
  })

  const ativoMutation = useMutation({
    mutationFn: ({ id, ativo }: { id: number; ativo: boolean }) => usuariosApi.alterarAtivo(id, ativo),
    onSuccess: () => {
      setErros([])
      invalidar()
    },
    onError: (error) => setErros(mensagensDeErro(error, 'Não foi possível alterar o status.')),
  })

  const usuarios = usuariosQuery.data ?? []
  const salvando = roleMutation.isPending || ativoMutation.isPending

  function ehEuMesmo(usuario: UsuarioResponse) {
    return usuario.email === sessao?.email
  }

  return (
    <AppLayout>
      <PageHeader
        titulo="Usuários"
        subtitulo="Permissões e acesso da equipe. Alterar permissão ou desativar encerra a sessão do usuário."
      />

      <div className="mb-4">
        <ErrorList mensagens={erros} />
      </div>

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Nome</th>
              <th>E-mail</th>
              <th>Permissão</th>
              <th>Status</th>
              <th>Desde</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={6}
              pending={usuariosQuery.isPending}
              error={usuariosQuery.error}
              empty={usuariosQuery.isSuccess && usuarios.length === 0}
              textoCarregando="Carregando usuários…"
              textoErro="Não foi possível carregar os usuários."
              textoVazio="Nenhum usuário cadastrado."
            />
            {usuarios.map((usuario) => (
              <tr key={usuario.id}>
                <td className="font-semibold">
                  {usuario.nome}
                  {ehEuMesmo(usuario) && (
                    <span className="ml-2 text-xs font-normal" style={{ color: mutedText }}>
                      (você)
                    </span>
                  )}
                </td>
                <td>{usuario.email}</td>
                <td>
                  <select
                    className="input"
                    style={{ borderRadius: 0, width: 140 }}
                    value={usuario.role}
                    disabled={salvando || ehEuMesmo(usuario)}
                    onChange={(e) =>
                      roleMutation.mutate({ id: usuario.id, role: e.target.value as Role })
                    }
                  >
                    {ROLES.map((role) => (
                      <option key={role} value={role}>{role}</option>
                    ))}
                  </select>
                </td>
                <td>
                  <span className={usuario.ativo ? 'tag tag-accent' : 'tag tag-neutral'}>
                    {usuario.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td>{formatDate(usuario.dataInclusao)}</td>
                <td>
                  <button
                    type="button"
                    className="btn btn-secondary"
                    style={{ borderRadius: 0 }}
                    disabled={salvando || ehEuMesmo(usuario)}
                    onClick={() => ativoMutation.mutate({ id: usuario.id, ativo: !usuario.ativo })}
                  >
                    {usuario.ativo ? 'Desativar' : 'Ativar'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <p className="mt-4 text-[13px]" style={{ color: mutedText }}>
        A empresa precisa ter sempre ao menos um administrador ativo — a API recusa rebaixar ou
        desativar o último. Sua própria conta não pode ser alterada por aqui.
      </p>
    </AppLayout>
  )
}
