import { http, unwrap } from './http'
import type {
  ApiResponse,
  CustoFiltro,
  LancamentoCustoResponse,
  ResultadoPaginado,
  ResumoCustosResponse,
} from './types'

/**
 * Leitura consolidada dos custos. Não há escrita aqui: o valor continua sendo lançado em
 * `/abastecimento` e em `/manutencao/{id}/concluir` — estes dois endpoints só unem e somam.
 *
 * Restrito à gestão (403 para a role `Motorista`), então a tela não precisa esconder valores.
 */
export const custosApi = {
  /** Lista paginada, mais recentes primeiro. Já vem ordenada — não reordenar no cliente. */
  async consultar(filtro: CustoFiltro = {}): Promise<ResultadoPaginado<LancamentoCustoResponse>> {
    const { data } = await http.get<ApiResponse<ResultadoPaginado<LancamentoCustoResponse>>>('/custo', {
      params: {
        pagina: filtro.pagina,
        tamanhoPagina: filtro.tamanhoPagina,
        veiculoId: filtro.veiculoId,
        motoristaId: filtro.motoristaId,
        origem: filtro.origem,
        de: filtro.de,
        ate: filtro.ate,
      },
    })
    return unwrap(data)
  },

  /** Totais do período somados no banco: por origem, por veículo e por mês. */
  async resumo(filtro: CustoFiltro = {}): Promise<ResumoCustosResponse> {
    const { data } = await http.get<ApiResponse<ResumoCustosResponse>>('/custo/resumo', {
      params: {
        veiculoId: filtro.veiculoId,
        motoristaId: filtro.motoristaId,
        origem: filtro.origem,
        de: filtro.de,
        ate: filtro.ate,
      },
    })
    return unwrap(data)
  },
}
