import { http, unwrap } from './http'
import type {
  ApiResponse,
  TipoManutencaoRequest,
  TipoManutencaoResponse,
  TipoManutencaoUpdateRequest,
} from './types'

export const tiposManutencaoApi = {
  /**
   * `apenasAtivos: true` no dropdown de agendamento; `false` (default da API) na
   * tela do catálogo, que também precisa mostrar os inativos para reativá-los.
   */
  async getAll(apenasAtivos = false): Promise<TipoManutencaoResponse[]> {
    const { data } = await http.get<ApiResponse<TipoManutencaoResponse[]>>('/tipomanutencao', {
      params: apenasAtivos ? { apenasAtivos: true } : undefined,
    })
    return unwrap(data)
  },
  async getById(id: number): Promise<TipoManutencaoResponse> {
    const { data } = await http.get<ApiResponse<TipoManutencaoResponse>>(`/tipomanutencao/${id}`)
    return unwrap(data)
  },
  async create(body: TipoManutencaoRequest): Promise<TipoManutencaoResponse> {
    const { data } = await http.post<ApiResponse<TipoManutencaoResponse>>('/tipomanutencao', body)
    return unwrap(data)
  },
  async update(id: number, body: TipoManutencaoUpdateRequest): Promise<TipoManutencaoResponse> {
    const { data } = await http.put<ApiResponse<TipoManutencaoResponse>>(`/tipomanutencao/${id}`, body)
    return unwrap(data)
  },
  /** Tipo já referenciado por alguma manutenção → 422 ("inative-o"). */
  async remove(id: number): Promise<void> {
    await http.delete(`/tipomanutencao/${id}`)
  },
}
