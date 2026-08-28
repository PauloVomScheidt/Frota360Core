import { http, unwrap } from './http'
import type { ApiResponse, AuditoriaFiltro, LogAuditoriaResponse, ResultadoPaginado } from './types'

export const auditoriaApi = {
  /**
   * Trilha da empresa, mais recentes primeiro. **Somente leitura e só Admin** — não há
   * endpoint que altere ou apague uma linha, nem para ele.
   *
   * Único endpoint paginado da API: a resposta vem em `ResultadoPaginado`, dentro do
   * `dados` do envelope de sempre. Os filtros vão para o servidor, como em `/manutencao`.
   */
  async consultar(filtro: AuditoriaFiltro = {}): Promise<ResultadoPaginado<LogAuditoriaResponse>> {
    const { data } = await http.get<ApiResponse<ResultadoPaginado<LogAuditoriaResponse>>>('/auditoria', {
      params: {
        pagina: filtro.pagina,
        tamanhoPagina: filtro.tamanhoPagina,
        entidade: filtro.entidade,
        acao: filtro.acao,
        usuarioId: filtro.usuarioId,
        de: filtro.de,
        ate: filtro.ate,
      },
    })
    return unwrap(data)
  },
}
