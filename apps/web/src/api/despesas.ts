import { http, unwrap } from './http'
import type {
  ApiResponse,
  AtualizarDespesaRequest,
  DespesaFiltro,
  DespesaRequest,
  DespesaResponse,
} from './types'

/**
 * Custos avulsos — pedágio, multa, IPVA, seguro. Fechado na gestão: a role `Motorista`
 * recebe 403 em todos os métodos, então a tela não precisa esconder valores.
 */
export const despesasApi = {
  /** Já vem da mais recente para a mais antiga — não reordenar no cliente. */
  async getAll(filtro: DespesaFiltro = {}): Promise<DespesaResponse[]> {
    const { data } = await http.get<ApiResponse<DespesaResponse[]>>('/despesa', {
      params: {
        veiculoId: filtro.veiculoId,
        motoristaId: filtro.motoristaId,
        tipoDespesaId: filtro.tipoDespesaId,
        de: filtro.de,
        ate: filtro.ate,
      },
    })
    return unwrap(data)
  },

  async getById(id: number): Promise<DespesaResponse> {
    const { data } = await http.get<ApiResponse<DespesaResponse>>(`/despesa/${id}`)
    return unwrap(data)
  },

  /** 422 quando o veículo, o tipo ou o motorista não existem na empresa, ou o tipo está inativo. */
  async create(body: DespesaRequest): Promise<DespesaResponse> {
    const { data } = await http.post<ApiResponse<DespesaResponse>>('/despesa', body)
    return unwrap(data)
  },

  /** Corrige todos os campos, inclusive veículo, tipo e motorista. */
  async update(id: number, body: AtualizarDespesaRequest): Promise<DespesaResponse> {
    const { data } = await http.put<ApiResponse<DespesaResponse>>(`/despesa/${id}`, body)
    return unwrap(data)
  },

  /** Admin **e Supervisor** — exceção deliberada à regra de que só o Admin exclui. */
  async remove(id: number): Promise<void> {
    await http.delete(`/despesa/${id}`)
  },
}
