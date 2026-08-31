import { http, unwrap } from './http'
import { tokenStorage } from './tokenStorage'
import type {
  AceitarConviteRequest,
  ApiResponse,
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

/** Anônimo: cria a conta na empresa/role do convite e já devolve a sessão autenticada. */
export async function aceitarConvite(body: AceitarConviteRequest): Promise<SessaoResponse> {
  const { data } = await http.post<ApiResponse<SessaoResponse>>('/convite/aceitar', body)
  const sessao = unwrap(data)
  tokenStorage.setSession(sessao)
  return sessao
}
