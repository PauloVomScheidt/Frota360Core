import type { Role } from '../api/types'

export const ROLES: Role[] = ['Admin', 'Supervisor', 'Operador', 'Motorista']

export const DESCRICAO_ROLE: Record<Role, string> = {
  Admin: 'Acesso total: usuários, convites e exclusões.',
  Supervisor: 'Cadastra e edita veículos, rotas e manutenções.',
  Operador: 'Visualiza a frota e gerencia rotas.',
  Motorista: 'Abre e encerra as próprias rotas; vê veículos e manutenções sem editar.',
}

/** Todos os papéis menos o motorista — quem opera o painel da frota. */
const gestao = (role?: Role) => role !== undefined && role !== 'Motorista'

/**
 * Matriz de permissões da API (§5 do CONTEXTO). O servidor é a autoridade —
 * isto existe só para não oferecer ao usuário ações que resultariam em 403.
 *
 * As entradas `ver*` são **por tela**, não um gate único de "painel": o motorista
 * enxerga veículos e manutenções, então um booleano só de "é gestão" seria mentira.
 */
export const pode = {
  gerenciarUsuarios: (role?: Role) => role === 'Admin',
  gerenciarConvites: (role?: Role) => role === 'Admin',
  /** Trilha de auditoria (`/auditoria`): só o Admin enxerga o que a equipe alterou. */
  verAuditoria: (role?: Role) => role === 'Admin',
  editarCadastros: (role?: Role) => role === 'Admin' || role === 'Supervisor',
  excluir: (role?: Role) => role === 'Admin',

  // ----- Visibilidade de tela -----
  verDashboard: gestao,
  verMotoristas: gestao,
  verRotas: gestao,
  /** Leitura da frota: o motorista escolhe o veículo ao abrir rota e consulta o odômetro. */
  verVeiculos: (role?: Role) => role !== undefined,
  /** Leitura do plano de manutenção: ele precisa saber o estado do veículo que vai pegar. */
  verManutencoes: (role?: Role) => role !== undefined,
  /** Tela `/minhas-rotas`: a única exclusiva do motorista. */
  verMinhasRotas: (role?: Role) => role === 'Motorista',
  /**
   * Abastecimento é a única tela que **todo mundo lê e escreve**: quem abastece na estrada
   * é o motorista, no pátio é o operador. O recorte de quem vê o quê é do servidor — a
   * gestão recebe a frota inteira, o motorista só os próprios lançamentos.
   */
  verAbastecimentos: (role?: Role) => role !== undefined,

  // ----- Ações -----
  /** Cadastrar, editar e encerrar rota na tela de gestão (`/rotas`). */
  editarRotas: gestao,
  /** Criar, editar e concluir manutenções — mesma régua de veículos. */
  editarManutencoes: (role?: Role) => role === 'Admin' || role === 'Supervisor',
  /** Manter o catálogo de tipos de manutenção (todos podem apenas visualizar). */
  editarTiposManutencao: (role?: Role) => role === 'Admin' || role === 'Supervisor',
  /**
   * Lançar e corrigir abastecimento — todos os papéis. O servidor é quem barra corrigir o
   * lançamento de outra pessoa (404 para o motorista), não esta entrada.
   */
  lancarAbastecimento: (role?: Role) => role !== undefined,
}

/**
 * Para onde mandar alguém que acabou de entrar — ou que caiu numa rota fechada
 * para o papel dele. O motorista não tem acesso ao painel de gestão, então
 * redirecioná-lo para `/dashboard` viraria um pingue-pongue entre guards.
 */
export function rotaInicial(role?: Role): string {
  return pode.verMinhasRotas(role) ? '/minhas-rotas' : '/dashboard'
}
