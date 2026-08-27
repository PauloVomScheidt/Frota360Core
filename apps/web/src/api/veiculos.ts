import { http, unwrap } from './http'
import type { ApiResponse, VeiculoRequest, VeiculoResponse } from './types'

export const veiculosApi = {
  async getAll(): Promise<VeiculoResponse[]> {
    const { data } = await http.get<ApiResponse<VeiculoResponse[]>>('/veiculo')
    return unwrap(data)
  },
  async getById(id: number): Promise<VeiculoResponse> {
    const { data } = await http.get<ApiResponse<VeiculoResponse>>(`/veiculo/${id}`)
    return unwrap(data)
  },
  async create(body: VeiculoRequest): Promise<VeiculoResponse> {
    const { data } = await http.post<ApiResponse<VeiculoResponse>>('/veiculo', body)
    return unwrap(data)
  },
  async update(id: number, body: VeiculoRequest): Promise<VeiculoResponse> {
    const { data } = await http.put<ApiResponse<VeiculoResponse>>(`/veiculo/${id}`, body)
    return unwrap(data)
  },
  async remove(id: number): Promise<void> {
    await http.delete(`/veiculo/${id}`)
  },
}
