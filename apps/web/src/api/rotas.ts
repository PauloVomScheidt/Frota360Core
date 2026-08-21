import { http, unwrap } from './http'
import type { ApiResponse, RotaRequest, RotaResponse } from './types'

export const rotasApi = {
  async getAll(): Promise<RotaResponse[]> {
    const { data } = await http.get<ApiResponse<RotaResponse[]>>('/rota')
    return unwrap(data)
  },
  async getById(id: number): Promise<RotaResponse> {
    const { data } = await http.get<ApiResponse<RotaResponse>>(`/rota/${id}`)
    return unwrap(data)
  },
  async create(body: RotaRequest): Promise<RotaResponse> {
    const { data } = await http.post<ApiResponse<RotaResponse>>('/rota', body)
    return unwrap(data)
  },
  async update(id: number, body: RotaRequest): Promise<RotaResponse> {
    const { data } = await http.put<ApiResponse<RotaResponse>>(`/rota/${id}`, body)
    return unwrap(data)
  },
  async remove(id: number): Promise<void> {
    await http.delete(`/rota/${id}`)
  },
}
