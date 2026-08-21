import { http, unwrap } from './http'
import { tokenStorage } from './tokenStorage'
import type {
  ApiResponse,
  AuthResponse,
  EsqueciSenhaRequest,
  LoginRequest,
  RedefinirSenhaRequest,
} from './types'

// Não existe registro público: contas nascem por convite (ver ./convites.ts).

export async function login(body: LoginRequest): Promise<AuthResponse> {
  const { data } = await http.post<ApiResponse<AuthResponse>>('/auth/login', body)
  const auth = unwrap(data)
  tokenStorage.setSession(auth)
  return auth
}

export async function logout(): Promise<void> {
  try {
    await http.post('/auth/logout')
  } finally {
    tokenStorage.clear()
  }
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
