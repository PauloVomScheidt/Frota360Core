import { http, unwrap } from './http'
import type { ApiResponse, MotoristaResponse } from './types'

/**
 * Somente leitura: um motorista é um usuário com a role `Motorista`, então quem
 * concede e remove o acesso é `convitesApi` / `usuariosApi`, não este módulo.
 */
export const motoristasApi = {
  async getAll(): Promise<MotoristaResponse[]> {
    const { data } = await http.get<ApiResponse<MotoristaResponse[]>>('/motorista')
    return unwrap(data)
  },
  async getById(id: number): Promise<MotoristaResponse> {
    const { data } = await http.get<ApiResponse<MotoristaResponse>>(`/motorista/${id}`)
    return unwrap(data)
  },
}
