import { http, unwrap } from './http'
import type {
  ApiResponse,
  ConcluirManutencaoRequest,
  ManutencaoFiltro,
  ManutencaoRequest,
  ManutencaoResponse,
  ResultadoPaginado,
  ResumoManutencoes,
} from './types'

export const manutencoesApi = {
  /**
   * A lista já vem ordenada pelo servidor (pendentes primeiro, vencendo antes no
   * topo) — não reordenar no cliente.
   */
  async getAll(filtro: ManutencaoFiltro = {}): Promise<ResultadoPaginado<ManutencaoResponse>> {
    const { data } = await http.get<ApiResponse<ResultadoPaginado<ManutencaoResponse>>>('/manutencao', {
      params: {
        veiculoId: filtro.veiculoId,
        status: filtro.status,
        de: filtro.de,
        ate: filtro.ate,
        pagina: filtro.pagina,
        tamanhoPagina: filtro.tamanhoPagina,
      },
    })
    return unwrap(data)
  },
  /** Só gestão (403 para o motorista). Alimenta o bloco "Precisa de atenção" do dashboard. */
  async resumo(): Promise<ResumoManutencoes> {
    const { data } = await http.get<ApiResponse<ResumoManutencoes>>('/manutencao/resumo')
    return unwrap(data)
  },
  async getById(id: number): Promise<ManutencaoResponse> {
    const { data } = await http.get<ApiResponse<ManutencaoResponse>>(`/manutencao/${id}`)
    return unwrap(data)
  },
  async create(body: ManutencaoRequest): Promise<ManutencaoResponse> {
    const { data } = await http.post<ApiResponse<ManutencaoResponse>>('/manutencao', body)
    return unwrap(data)
  },
  /** Só registro pendente aceita edição; realizada/cancelada → 422. */
  async update(id: number, body: ManutencaoRequest): Promise<ManutencaoResponse> {
    const { data } = await http.put<ApiResponse<ManutencaoResponse>>(`/manutencao/${id}`, body)
    return unwrap(data)
  },
  /**
   * Efeito colateral: quando `quilometragemRealizada` > quilometragem atual, o
   * veículo é atualizado (nunca retrocede). Invalidar também o cache de veículos.
   */
  async concluir(id: number, body: ConcluirManutencaoRequest): Promise<ManutencaoResponse> {
    const { data } = await http.post<ApiResponse<ManutencaoResponse>>(`/manutencao/${id}/concluir`, body)
    return unwrap(data)
  },
  async remove(id: number): Promise<void> {
    await http.delete(`/manutencao/${id}`)
  },
}
