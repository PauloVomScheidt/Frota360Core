import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usuariosApi } from '../api/usuarios'
import { mensagensDeErro } from '../api/errors'
import type { Role, UsuarioResponse } from '../api/types'
import { ROLES } from '../auth/permissions'
import { useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { ConfirmDialog, TableStates } from '../components/Table'
import { formatDate } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

/**
 * As duas ações desta tela mexem na conta de outra pessoa e derrubam a sessão dela.
 * Um estado só garante que nunca haja dois diálogos disputando a atenção.
 * Reativar fica de fora: devolve acesso e não revoga nada.
 */
type Confirmacao =
  | { tipo: 'role'; usuario: UsuarioResponse; novaRole: Role }
  | { tipo: 'desativar'; usuario: UsuarioResponse }

/** O que muda de fato para a pessoa — não só "você tem certeza?". */
function consequenciasDaTroca(usuario: UsuarioResponse, novaRole: Role): string {
  const partes = [
    `${usuario.nome} passa de ${usuario.role} para ${novaRole}.`,
    'A sessão dele será encerrada: ele precisa entrar de novo para o novo perfil valer.',
  ]

  if (novaRole === 'Motorista') {
    partes.push(
      'Como Motorista, ele deixa de ver o painel da frota e passa a enxergar apenas as próprias rotas.',
    )
  }

  if (usuario.role === 'Motorista') {
    partes.push(
      'Ele deixa de abrir e encerrar rotas, e sai da lista de motoristas; as rotas já registradas continuam no histórico.',
    )
  }

  return partes.join(' ')
}

function consequenciasDaDesativacao(usuario: UsuarioResponse): string {
  const partes = [
    `${usuario.nome} perde o acesso: a sessão dele será encerrada e ele não conseguirá entrar até ser reativado aqui.`,
    'Nada é apagado — o histórico dele continua onde está.',
  ]

  if (usuario.role === 'Motorista') {
    partes.push(
      'Rotas em andamento continuam abertas: quem precisar encerrá-las faz isso pela tela de Rotas.',
    )
  }

  return partes.join(' ')
}

export function UsuariosPage() {
  const queryClient = useQueryClient()
  const sessao = useSession()
  const [erros, setErros] = useState<string[]>([])

  // Trocar permissão e desativar são fáceis demais de disparar sem querer — um esbarrão
  // no select, um clique errado de linha. A escolha vira intenção pendente até confirmar.
  const [confirmacao, setConfirmacao] = useState<Confirmacao | null>(null)
  const [errosConfirmacao, setErrosConfirmacao] = useState<string[]>([])

  const usuariosQuery = useQuery({ queryKey: ['usuarios'], queryFn: usuariosApi.getAll })

  const invalidar = () => queryClient.invalidateQueries({ queryKey: ['usuarios'] })

  function fecharConfirmacao() {
    setConfirmacao(null)
    setErrosConfirmacao([])
  }

  const roleMutation = useMutation({
    mutationFn: ({ id, role }: { id: number; role: Role }) => usuariosApi.alterarRole(id, role),
    onSuccess: () => {
      setErros([])
      fecharConfirmacao()
      invalidar()
    },
    // Ex.: rebaixar o último admin ativo → 422 com a mensagem da regra. Fica dentro do
    // diálogo, junto da ação que a provocou.
    onError: (error) =>
      setErrosConfirmacao(mensagensDeErro(error, 'Não foi possível alterar a permissão.')),
  })

  const ativoMutation = useMutation({
    mutationFn: ({ id, ativo }: { id: number; ativo: boolean }) => usuariosApi.alterarAtivo(id, ativo),
    onSuccess: () => {
      setErros([])
      fecharConfirmacao()
      invalidar()
    },
    // Só a desativação passa por diálogo, então o erro segue o caminho da ação. Ler
    // `ativo` do payload é mais confiável do que inspecionar o estado do componente.
    onError: (error, { ativo }) => {
      const mensagens = mensagensDeErro(error, 'Não foi possível alterar o status.')
      if (ativo) setErros(mensagens)
      else setErrosConfirmacao(mensagens)
    },
  })

  function pedirTrocaDeRole(usuario: UsuarioResponse, novaRole: Role) {
    // O select é controlado por `usuario.role`: sem mutation, ele volta sozinho ao valor
    // atual — cancelar não deixa a tela mentindo sobre o estado.
    if (novaRole === usuario.role) return
    setErrosConfirmacao([])
    setConfirmacao({ tipo: 'role', usuario, novaRole })
  }

  function alternarAtivo(usuario: UsuarioResponse) {
    if (usuario.ativo) {
      setErrosConfirmacao([])
      setConfirmacao({ tipo: 'desativar', usuario })
      return
    }
    // Reativar devolve o acesso e não derruba sessão de ninguém: vai direto.
    ativoMutation.mutate({ id: usuario.id, ativo: true })
  }

  const usuarios = usuariosQuery.data ?? []
  const salvando = roleMutation.isPending || ativoMutation.isPending

  function ehEuMesmo(usuario: UsuarioResponse) {
    return usuario.email === sessao?.email
  }

  const dialogo =
    confirmacao?.tipo === 'role'
      ? {
          titulo: 'Alterar permissão',
          mensagem: consequenciasDaTroca(confirmacao.usuario, confirmacao.novaRole),
          textoConfirmar: `Alterar para ${confirmacao.novaRole}`,
          textoPendente: 'Alterando…',
          pending: roleMutation.isPending,
          onConfirmar: () =>
            roleMutation.mutate({ id: confirmacao.usuario.id, role: confirmacao.novaRole }),
        }
      : confirmacao?.tipo === 'desativar'
        ? {
            titulo: 'Desativar usuário',
            mensagem: consequenciasDaDesativacao(confirmacao.usuario),
            textoConfirmar: 'Desativar',
            textoPendente: 'Desativando…',
            pending: ativoMutation.isPending,
            onConfirmar: () => ativoMutation.mutate({ id: confirmacao.usuario.id, ativo: false }),
          }
        : null

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
                    onChange={(e) => pedirTrocaDeRole(usuario, e.target.value as Role)}
                  >
                    {ROLES.map((role) => (
                      <option key={role} value={role}>{role}</option>
                    ))}
                  </select>
                </td>
                <td>
                  <span className={usuario.ativo ? 'tag tag-success' : 'tag tag-neutral'}>
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
                    onClick={() => alternarAtivo(usuario)}
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

      {dialogo && (
        <ConfirmDialog
          {...dialogo}
          // Nenhuma das duas é destrutiva como uma exclusão — as duas se desfazem.
          variante="padrao"
          erros={errosConfirmacao}
          onCancelar={fecharConfirmacao}
        />
      )}
    </AppLayout>
  )
}
