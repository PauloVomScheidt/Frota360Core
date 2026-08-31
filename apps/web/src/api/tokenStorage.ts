import type { Role, SessaoResponse } from './types'

// O ":v1" existe para o dia em que o formato do StoredUser mudar: uma chave sem versão faria
// o JSON.parse de uma sessão salva no formato antigo quebrar (ou, pior, "funcionar" com campos
// errados). Bastando trocar para ":v2" nesse dia, sessões antigas somem em vez de crashar.
const USER_KEY = 'frota360.user:v1'

export interface StoredUser {
  nome: string
  email: string
  role: Role
}

// Token e refresh token não passam por aqui: o servidor os entrega em cookie HttpOnly, fora
// do alcance do JavaScript — só identidade (para exibição na UI) fica no localStorage.
export const tokenStorage = {
  /** Grava a identidade a partir da SessaoResponse (login, refresh, aceite de convite). */
  setSession(sessao: SessaoResponse) {
    localStorage.setItem(
      USER_KEY,
      JSON.stringify({ nome: sessao.nome, email: sessao.email, role: sessao.role } satisfies StoredUser),
    )
  },

  /**
   * Corrige só o nome guardado na sessão, depois de o usuário editar o próprio perfil.
   * O claim `name` do JWT segue o antigo até o próximo refresh — sem isto, o header
   * exibiria o nome velho até o token girar.
   */
  atualizarNome(nome: string) {
    const atual = this.getUser()
    if (!atual) return
    localStorage.setItem(USER_KEY, JSON.stringify({ ...atual, nome } satisfies StoredUser))
  },

  getUser(): StoredUser | null {
    const raw = localStorage.getItem(USER_KEY)
    if (!raw) return null
    try {
      return JSON.parse(raw) as StoredUser
    } catch {
      return null
    }
  },

  clear() {
    localStorage.removeItem(USER_KEY)
  },
}
