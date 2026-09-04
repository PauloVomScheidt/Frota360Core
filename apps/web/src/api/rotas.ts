import { http, unwrap } from './http'
import type {
  AbrirMinhaRotaRequest,
  ApiResponse,
  CriarRotaRequest,
  EncerrarRotaRequest,
  ResultadoPaginado,
  ResumoRotas,
  RotaFiltro,
  RotaRequest,
  RotaResponse,
} from './types'

export const rotasApi = {
  async getAll(filtro: RotaFiltro = {}): Promise<ResultadoPaginado<RotaResponse>> {
    const { data } = await http.get<ApiResponse<ResultadoPaginado<RotaResponse>>>('/rota', {
      params: { pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina, ativo: filtro.ativo },
    })
    return unwrap(data)
  },
  /**
   * Rotas do motorista logado (role `Motorista`). Não recebe id: o motorista vem da
   * claim do JWT, então não há como pedir as rotas de outra pessoa. Para as demais
   * roles a API responde 403 — elas usam `getAll`.
   */
  async getMinhas(filtro: RotaFiltro = {}): Promise<ResultadoPaginado<RotaResponse>> {
    const { data } = await http.get<ApiResponse<ResultadoPaginado<RotaResponse>>>('/rota/minhas', {
      params: { pagina: filtro.pagina, tamanhoPagina: filtro.tamanhoPagina, ativo: filtro.ativo },
    })
    return unwrap(data)
  },
  /**
   * Quantidade e km somado das rotas **encerradas** no período. É o KPI "Km da frota" do
   * dashboard: com a listagem paginada, somar `kmPercorrido` no cliente deixou de ser possível.
   */
  async resumo(de: string, ate: string): Promise<ResumoRotas> {
    const { data } = await http.get<ApiResponse<ResumoRotas>>('/rota/resumo', {
      params: { de, ate },
    })
    return unwrap(data)
  },
  async getById(id: number): Promise<RotaResponse> {
    const { data } = await http.get<ApiResponse<RotaResponse>>(`/rota/${id}`)
    return unwrap(data)
  },
  /**
   * Efeito colateral: `kmInicial` acima da quilometragem atual do veículo avança o
   * odômetro já na abertura (o veículo rodou fora do sistema). Invalidar `['veiculos']`.
   *
   * Para a role `Motorista` a API ignora o `codigoMotorista` do corpo e usa o da claim —
   * ele só abre rota para si mesmo.
   */
  async create(body: CriarRotaRequest): Promise<RotaResponse> {
    const { data } = await http.post<ApiResponse<RotaResponse>>('/rota', body)
    return unwrap(data)
  },
  /**
   * Mesmo endpoint do `create`, para a role `Motorista`: o corpo não leva
   * `codigoMotorista` porque a API usa o da claim. Invalidar `['rotas','minhas']` e
   * `['veiculos']`.
   */
  async abrirMinha(body: AbrirMinhaRotaRequest): Promise<RotaResponse> {
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
