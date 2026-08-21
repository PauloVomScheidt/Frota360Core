import type { Role } from '../api/types'

export const ROLES: Role[] = ['Admin', 'Supervisor', 'Operador']

export const DESCRICAO_ROLE: Record<Role, string> = {
  Admin: 'Acesso total: usuários, convites e exclusões.',
  Supervisor: 'Cadastra e edita motoristas, veículos e rotas.',
  Operador: 'Visualiza tudo e gerencia rotas.',
}

/**
 * Matriz de permissões da API (§5 do CONTEXTO). O servidor é a autoridade —
 * isto existe só para não oferecer ao usuário ações que resultariam em 403.
 */
export const pode = {
  gerenciarUsuarios: (role?: Role) => role === 'Admin',
  gerenciarConvites: (role?: Role) => role === 'Admin',
  editarCadastros: (role?: Role) => role === 'Admin' || role === 'Supervisor',
  excluir: (role?: Role) => role === 'Admin',
  editarRotas: (role?: Role) => role !== undefined,
}
