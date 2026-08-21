import { http, unwrap } from './http'
import type { ApiResponse, Role, UsuarioResponse } from './types'

// Todos os endpoints exigem role Admin (403 com envelope caso contrário).
export const usuariosApi = {
  async getAll(): Promise<UsuarioResponse[]> {
    const { data } = await http.get<ApiResponse<UsuarioResponse[]>>('/usuario')
    return unwrap(data)
  },
  /** Revoga a sessão do alvo. Rebaixar o último admin ativo → 422. */
  async alterarRole(id: number, role: Role): Promise<void> {
    await http.put(`/usuario/${id}/role`, { role })
  },
  /** Desativar revoga a sessão do alvo. Desativar o último admin ativo → 422. */
  async alterarAtivo(id: number, ativo: boolean): Promise<void> {
    await http.put(`/usuario/${id}/ativo`, { ativo })
  },
}
