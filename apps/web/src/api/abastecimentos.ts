import { http, unwrap } from './http'
import type {
  AbastecimentoAnteriorResponse,
  AbastecimentoFiltro,
  AbastecimentoRequest,
  AbastecimentoResponse,
  ApiResponse,
  AtualizarAbastecimentoRequest,
  ResultadoPaginado,
  ResumoLancamentos,
} from './types'

/** Os filtros que a listagem e o resumo compartilham — os dois têm de ver o mesmo recorte. */
function paramsDoFiltro(filtro: AbastecimentoFiltro) {
  return {
    veiculoId: filtro.veiculoId,
    motoristaId: filtro.motoristaId,
    de: filtro.de,
    ate: filtro.ate,
  }
}

export const abastecimentosApi = {
  /**
   * A gestão recebe a frota inteira e pode filtrar por motorista; a role `Motorista` recebe
   * só o que é dela — inclusive o que a gestão lançou **para** ela. O recorte é **do
   * servidor** (sai do `sub` do token), e um `motoristaId` enviado por um motorista é
   * sobrescrito: filtro de cliente não é isolamento.
   *
   * A lista já vem do mais recente para o mais antigo — não reordenar no cliente.
   */
  async getAll(filtro: AbastecimentoFiltro = {}): Promise<ResultadoPaginado<AbastecimentoResponse>> {
    const { data } = await http.get<ApiResponse<ResultadoPaginado<AbastecimentoResponse>>>('/abastecimento', {
      params: { ...paramsDoFiltro(filtro), pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina },
    })
    return unwrap(data)
  },

  /**
   * Contagem e soma do **filtro inteiro**, para o rodapé — não da página. Recebe o mesmo
   * filtro da listagem e ignora a paginação; obedece ao mesmo recorte de motorista.
   */
  async resumo(filtro: AbastecimentoFiltro = {}): Promise<ResumoLancamentos> {
    const { data } = await http.get<ApiResponse<ResumoLancamentos>>('/abastecimento/resumo', {
      params: paramsDoFiltro(filtro),
    })
    return unwrap(data)
  },

  /**
   * A referência da estimativa de km/l: o abastecimento de maior odômetro **abaixo** do
   * informado, naquele veículo. `null` no primeiro abastecimento — não ter referência é
   * resposta válida, não erro.
   *
   * ⚠️ Enxerga o histórico do veículo sem recorte por motorista, e é isso que corrige o
   * km/l inflado que o motorista via quando o anterior daquele caminhão era de outra
   * pessoa. Por isso a resposta traz só data e odômetro.
   */
  async anterior(veiculoId: number, odometro: number, ignorarId?: number): Promise<AbastecimentoAnteriorResponse | null> {
    const { data } = await http.get<ApiResponse<AbastecimentoAnteriorResponse | null>>('/abastecimento/anterior', {
      params: { veiculoId, odometro, ignorarId },
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
