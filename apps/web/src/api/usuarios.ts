import { http, unwrap } from './http'
import type { ApiResponse, AtualizarPerfilRequest, Role, UsuarioResponse } from './types'

// A gestão de usuários exige role Admin (403 com envelope caso contrário). As duas
// últimas entradas são a exceção: perfil é autoatendimento, aberto a qualquer autenticado.
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

  /** Cadastro do próprio usuário logado — `getAll` é Admin-only e não serve ao Motorista. */
  async getPerfil(): Promise<UsuarioResponse> {
    const { data } = await http.get<ApiResponse<UsuarioResponse>>('/usuario/perfil')
    return unwrap(data)
  },

  /** CPF já usado por outro usuário da mesma empresa → 422. */
  async atualizarPerfil(payload: AtualizarPerfilRequest): Promise<UsuarioResponse> {
    const { data } = await http.put<ApiResponse<UsuarioResponse>>('/usuario/perfil', payload)
    return unwrap(data)
  },
}
