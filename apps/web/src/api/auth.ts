import { http, unwrap } from './http'
import { tokenStorage } from './tokenStorage'
import type {
  ApiResponse,
  EsqueciSenhaRequest,
  LoginRequest,
  RedefinirSenhaRequest,
  SessaoResponse,
} from './types'

// Não existe registro público: contas nascem por convite (ver ./convites.ts).

export async function login(body: LoginRequest): Promise<SessaoResponse> {
  const { data } = await http.post<ApiResponse<SessaoResponse>>('/auth/login', body)
  const sessao = unwrap(data)
  tokenStorage.setSession(sessao)
  return sessao
}

/**
 * Sair não depende do servidor: com a API fora do ar o erro é engolido e a sessão local sai do
 * mesmo jeito — propagá-lo deixaria quem chama (o `AppLayout`) parado antes de ir para /login.
 */
export async function logout(): Promise<void> {
  await http.post('/auth/logout').catch(() => undefined)
  tokenStorage.clear()
}

/** A API sempre responde 200 neutro — nunca revela se o e-mail existe. */
export async function esqueciSenha(body: EsqueciSenhaRequest): Promise<string> {
  const { data } = await http.post<ApiResponse<null>>('/auth/esqueci-senha', body)
  return data.mensagem
}

/** Troca a senha pelo token do e-mail; derruba as sessões antigas no servidor. */
export async function redefinirSenha(body: RedefinirSenhaRequest): Promise<void> {
  await http.post<ApiResponse<null>>('/auth/redefinir-senha', body)
}
