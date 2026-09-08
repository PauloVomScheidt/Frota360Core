import { http, unwrap } from './http'
import type {
  ApiResponse,
  TipoDespesaRequest,
  TipoDespesaResponse,
  TipoDespesaUpdateRequest,
} from './types'

export const tiposDespesaApi = {
  /**
   * `apenasAtivos` no seletor de lançamento (tipo aposentado não recebe despesa nova);
   * sem o parâmetro na tela do catálogo, que precisa listar os inativos para reativá-los.
   */
  async getAll(apenasAtivos = false): Promise<TipoDespesaResponse[]> {
    const { data } = await http.get<ApiResponse<TipoDespesaResponse[]>>('/tipodespesa', {
      params: { apenasAtivos },
    })
    return unwrap(data)
  },

  async getById(id: number): Promise<TipoDespesaResponse> {
    const { data } = await http.get<ApiResponse<TipoDespesaResponse>>(`/tipodespesa/${id}`)
    return unwrap(data)
  },

  /** Admin/Supervisor. Nome duplicado na empresa devolve 422. */
  async create(body: TipoDespesaRequest): Promise<TipoDespesaResponse> {
    const { data } = await http.post<ApiResponse<TipoDespesaResponse>>('/tipodespesa', body)
    return unwrap(data)
  },

  async update(id: number, body: TipoDespesaUpdateRequest): Promise<TipoDespesaResponse> {
    const { data } = await http.put<ApiResponse<TipoDespesaResponse>>(`/tipodespesa/${id}`, body)
    return unwrap(data)
  },

  /** Só Admin. Tipo em uso devolve 422 pedindo para inativar. */
  async remove(id: number): Promise<void> {
    await http.delete(`/tipodespesa/${id}`)
  },
}
