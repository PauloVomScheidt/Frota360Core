import { http, unwrap } from './http'
import { tokenStorage } from './tokenStorage'
import type { ApiResponse, AuthResponse, LoginRequest, RegisterRequest } from './types'

export async function login(body: LoginRequest): Promise<AuthResponse> {
  const { data } = await http.post<ApiResponse<AuthResponse>>('/auth/login', body)
  const auth = unwrap(data)
  tokenStorage.set(auth.token, auth.refreshToken)
  tokenStorage.setUser({ nome: auth.nome, email: auth.email })
  return auth
}

export async function register(body: RegisterRequest): Promise<AuthResponse> {
  const { data } = await http.post<ApiResponse<AuthResponse>>('/auth/register', body)
  const auth = unwrap(data)
  tokenStorage.set(auth.token, auth.refreshToken)
  tokenStorage.setUser({ nome: auth.nome, email: auth.email })
  return auth
}

export async function logout(): Promise<void> {
  try {
    await http.post('/auth/logout')
  } finally {
    tokenStorage.clear()
  }
}
