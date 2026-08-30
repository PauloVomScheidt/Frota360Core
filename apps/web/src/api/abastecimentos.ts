import { http, unwrap } from './http'
import type {
  AbastecimentoFiltro,
  AbastecimentoRequest,
  AbastecimentoResponse,
  ApiResponse,
  AtualizarAbastecimentoRequest,
} from './types'

export const abastecimentosApi = {
  /**
   * A gestão recebe a frota inteira e pode filtrar por motorista; a role `Motorista` recebe
   * só o que é dela — inclusive o que a gestão lançou **para** ela. O recorte é **do
   * servidor** (sai do `sub` do token), e um `motoristaId` enviado por um motorista é
   * sobrescrito: filtro de cliente não é isolamento.
   *
   * A lista já vem do mais recente para o mais antigo — não reordenar no cliente.
   */
  async getAll(filtro: AbastecimentoFiltro = {}): Promise<AbastecimentoResponse[]> {
    const { data } = await http.get<ApiResponse<AbastecimentoResponse[]>>('/abastecimento', {
      params: {
        veiculoId: filtro.veiculoId,
        motoristaId: filtro.motoristaId,
        de: filtro.de,
        ate: filtro.ate,
      },
    })
    return unwrap(data)
  },

  async getById(id: number): Promise<AbastecimentoResponse> {
    const { data } = await http.get<ApiResponse<AbastecimentoResponse>>(`/abastecimento/${id}`)
    return unwrap(data)
  },

  /**
   * Não mexe no odômetro do veículo — o lançamento é só o gasto. Basta invalidar
   * `['abastecimentos']`.
   *
   * Devolve 422 quando um motorista com rota aberta manda um veículo diferente do da rota.
   */
  async create(body: AbastecimentoRequest): Promise<AbastecimentoResponse> {
    const { data } = await http.post<ApiResponse<AbastecimentoResponse>>('/abastecimento', body)
    return unwrap(data)
  },

  /** Corrige valor, data e observação. Veículo, motorista e rota não são editáveis. */
  async update(id: number, body: AtualizarAbastecimentoRequest): Promise<AbastecimentoResponse> {
    const { data } = await http.put<ApiResponse<AbastecimentoResponse>>(`/abastecimento/${id}`, body)
    return unwrap(data)
  },

  /** Só Admin. */
  async remove(id: number): Promise<void> {
    await http.delete(`/abastecimento/${id}`)
  },
}
