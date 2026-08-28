import type { AuthResponse, Role } from './types'

const TOKEN_KEY = 'frota360.token'
const REFRESH_KEY = 'frota360.refreshToken'
const USER_KEY = 'frota360.user'

export interface StoredUser {
  nome: string
  email: string
  role: Role
}

export const tokenStorage = {
  getToken: () => localStorage.getItem(TOKEN_KEY),
  getRefreshToken: () => localStorage.getItem(REFRESH_KEY),

  /** Grava tokens + identidade a partir do AuthResponse (login, refresh, aceite de convite). */
  setSession(auth: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, auth.token)
    localStorage.setItem(REFRESH_KEY, auth.refreshToken)
    localStorage.setItem(
      USER_KEY,
      JSON.stringify({ nome: auth.nome, email: auth.email, role: auth.role } satisfies StoredUser),
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
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(REFRESH_KEY)
    localStorage.removeItem(USER_KEY)
  },
}
