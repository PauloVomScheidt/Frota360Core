import { http, unwrap } from './http'
import { tokenStorage } from './tokenStorage'
import type {
  AceitarConviteRequest,
  ApiResponse,
  ConviteConsultadoResponse,
  ConviteResponse,
  CriarConviteRequest,
  SessaoResponse,
} from './types'

export const convitesApi = {
  /** Admin: cria o convite e dispara o e-mail. A resposta traz `linkConvite` para reenvio manual. */
  async criar(body: CriarConviteRequest): Promise<ConviteResponse> {
    const { data } = await http.post<ApiResponse<ConviteResponse>>('/convite', body)
    return unwrap(data)
  },
  /** Admin: lista os convites da empresa. */
  async getAll(): Promise<ConviteResponse[]> {
    const { data } = await http.get<ApiResponse<ConviteResponse[]>>('/convite')
    return unwrap(data)
  },
  /** Admin: cancela um convite pendente (já utilizado → 422). */
  async cancelar(id: number): Promise<void> {
    await http.delete(`/convite/${id}`)
  },
}

/**
 * Anônimo: lê o convite sem consumi-lo. Inválido, utilizado ou expirado → 404. POST para o
 * token ir no corpo, e não na URL.
 */
export async function consultarConvite(token: string): Promise<ConviteConsultadoResponse> {
  const { data } = await http.post<ApiResponse<ConviteConsultadoResponse>>('/convite/consultar', { token })
  return unwrap(data)
}

/** Anônimo: cria a conta na empresa/role do convite e já devolve a sessão autenticada. */
export async function aceitarConvite(body: AceitarConviteRequest): Promise<SessaoResponse> {
  const { data } = await http.post<ApiResponse<SessaoResponse>>('/convite/aceitar', body)
  const sessao = unwrap(data)
  tokenStorage.setSession(sessao)
  return sessao
}
