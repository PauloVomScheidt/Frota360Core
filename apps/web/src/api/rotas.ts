import { http, unwrap } from './http'
import type {
  ApiResponse,
  CriarRotaRequest,
  EncerrarRotaRequest,
  RotaRequest,
  RotaResponse,
} from './types'

export const rotasApi = {
  async getAll(): Promise<RotaResponse[]> {
    const { data } = await http.get<ApiResponse<RotaResponse[]>>('/rota')
    return unwrap(data)
  },
  async getById(id: number): Promise<RotaResponse> {
    const { data } = await http.get<ApiResponse<RotaResponse>>(`/rota/${id}`)
    return unwrap(data)
  },
  /**
   * Efeito colateral: `kmInicial` acima da quilometragem atual do veículo avança o
   * odômetro já na abertura (o veículo rodou fora do sistema). Invalidar `['veiculos']`.
   */
  async create(body: CriarRotaRequest): Promise<RotaResponse> {
    const { data } = await http.post<ApiResponse<RotaResponse>>('/rota', body)
    return unwrap(data)
  },
  /** Não altera `kmInicial`, `ativo` nem `dataFim` — encerrar é o único caminho para isso. */
  async update(id: number, body: RotaRequest): Promise<RotaResponse> {
    const { data } = await http.put<ApiResponse<RotaResponse>>(`/rota/${id}`, body)
    return unwrap(data)
  },
  /**
   * Transição de estado: grava `kmFinal`/`dataFim`, calcula `kmPercorrido` e avança o
   * odômetro do veículo (nunca retrocede). Invalidar `['rotas']`, `['veiculos']` e
   * `['manutencoes']` — a cadeia é rota → veículo → manutenção.
   */
  async encerrar(id: number, body: EncerrarRotaRequest): Promise<RotaResponse> {
    const { data } = await http.post<ApiResponse<RotaResponse>>(`/rota/${id}/encerrar`, body)
    return unwrap(data)
  },
  async remove(id: number): Promise<void> {
    await http.delete(`/rota/${id}`)
  },
}
