import { http, unwrap } from './http'
import type {
  ApiResponse,
  TipoCombustivelRequest,
  TipoCombustivelResponse,
  TipoCombustivelUpdateRequest,
} from './types'

export const tiposCombustivelApi = {
  /**
   * `apenasAtivos` no seletor de lançamento (combustível aposentado não recebe
   * abastecimento novo); sem o parâmetro na tela do catálogo, que precisa listar os
   * inativos para reativá-los.
   *
   * A leitura é aberta a todos os papéis — o motorista também lança abastecimento.
   */
  async getAll(apenasAtivos = false): Promise<TipoCombustivelResponse[]> {
    const { data } = await http.get<ApiResponse<TipoCombustivelResponse[]>>('/tipocombustivel', {
      params: { apenasAtivos },
    })
    return unwrap(data)
  },

  async getById(id: number): Promise<TipoCombustivelResponse> {
    const { data } = await http.get<ApiResponse<TipoCombustivelResponse>>(`/tipocombustivel/${id}`)
    return unwrap(data)
  },

  /** Admin/Supervisor. Nome duplicado na empresa devolve 422. */
  async create(body: TipoCombustivelRequest): Promise<TipoCombustivelResponse> {
    const { data } = await http.post<ApiResponse<TipoCombustivelResponse>>('/tipocombustivel', body)
    return unwrap(data)
  },

  async update(id: number, body: TipoCombustivelUpdateRequest): Promise<TipoCombustivelResponse> {
    const { data } = await http.put<ApiResponse<TipoCombustivelResponse>>(`/tipocombustivel/${id}`, body)
    return unwrap(data)
  },

  /** Só Admin. Combustível em uso devolve 422 pedindo para inativar. */
  async remove(id: number): Promise<void> {
    await http.delete(`/tipocombustivel/${id}`)
  },
}
