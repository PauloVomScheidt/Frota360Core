import { http, unwrap } from './http'
import type { ApiResponse, PostoRequest, PostoResponse, PostoUpdateRequest } from './types'

export const postosApi = {
  /**
   * A rede credenciada da empresa. `apenasAtivos` no seletor de lançamento — posto
   * descredenciado continua nomeando o histórico mas não recebe abastecimento novo.
   *
   * Como o catálogo de combustível, a leitura é aberta a todos os papéis.
   */
  async getAll(apenasAtivos = false): Promise<PostoResponse[]> {
    const { data } = await http.get<ApiResponse<PostoResponse[]>>('/posto', {
      params: { apenasAtivos },
    })
    return unwrap(data)
  },

  async getById(id: number): Promise<PostoResponse> {
    const { data } = await http.get<ApiResponse<PostoResponse>>(`/posto/${id}`)
    return unwrap(data)
  },

  /** Admin/Supervisor. Nome duplicado na empresa devolve 422. */
  async create(body: PostoRequest): Promise<PostoResponse> {
    const { data } = await http.post<ApiResponse<PostoResponse>>('/posto', body)
    return unwrap(data)
  },

  async update(id: number, body: PostoUpdateRequest): Promise<PostoResponse> {
    const { data } = await http.put<ApiResponse<PostoResponse>>(`/posto/${id}`, body)
    return unwrap(data)
  },

  /** Só Admin. Posto em uso devolve 422 pedindo para inativar. */
  async remove(id: number): Promise<void> {
    await http.delete(`/posto/${id}`)
  },
}
