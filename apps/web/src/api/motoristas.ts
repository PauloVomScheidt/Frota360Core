import { http, unwrap } from './http'
import type { ApiResponse, MotoristaRequest, MotoristaResponse } from './types'

export const motoristasApi = {
  async getAll(): Promise<MotoristaResponse[]> {
    const { data } = await http.get<ApiResponse<MotoristaResponse[]>>('/motorista')
    return unwrap(data)
  },
  async getById(id: number): Promise<MotoristaResponse> {
    const { data } = await http.get<ApiResponse<MotoristaResponse>>(`/motorista/${id}`)
    return unwrap(data)
  },
  async create(body: MotoristaRequest): Promise<MotoristaResponse> {
    const { data } = await http.post<ApiResponse<MotoristaResponse>>('/motorista', body)
    return unwrap(data)
  },
  async update(id: number, body: MotoristaRequest): Promise<MotoristaResponse> {
    const { data } = await http.put<ApiResponse<MotoristaResponse>>(`/motorista/${id}`, body)
    return unwrap(data)
  },
  async remove(id: number): Promise<void> {
    await http.delete(`/motorista/${id}`)
  },
}
