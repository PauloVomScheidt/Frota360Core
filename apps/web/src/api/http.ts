import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { tokenStorage } from './tokenStorage'
import type { ApiResponse, AuthResponse } from './types'

// Sem baseURL o axios cai em URLs relativas à origem do front, e o CloudFront responde o
// index.html do SPA — o erro chega como falha de parse de JSON, sem nenhuma pista de que a
// configuração está faltando. Melhor quebrar aqui.
const baseURL = import.meta.env.VITE_API_URL
if (!baseURL) {
  throw new Error(
    'VITE_API_URL não definida. Preencha .env.development ou .env.production antes do build ' +
      '(o valor precisa terminar em /api/v1).',
  )
}

export const http = axios.create({ baseURL })

http.interceptors.request.use((config) => {
  const token = tokenStorage.getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Rotas anônimas: um 401 aqui é credencial/token inválido, não sessão expirada,
// então não faz sentido tentar refresh nem derrubar o usuário para o login.
const ANONYMOUS_ROUTES = ['/auth/login', '/auth/refresh', '/auth/esqueci-senha', '/auth/redefinir-senha', '/convite/aceitar']

// A rotação de refresh token invalida o token anterior a cada uso, então
// dois refreshes simultâneos fariam o segundo falhar. O lock abaixo garante
// um único refresh em voo; as demais requisições 401 aguardam o mesmo.
let refreshInFlight: Promise<string> | null = null

async function refreshTokens(): Promise<string> {
  const refreshToken = tokenStorage.getRefreshToken()
  if (!refreshToken) throw new Error('Sem refresh token')

  // axios "cru" para não passar pelos interceptors deste cliente
  const { data } = await axios.post<ApiResponse<AuthResponse>>(
    `${baseURL}/auth/refresh`,
    { refreshToken },
  )
  if (!data.dados) throw new Error('Refresh sem dados')

  // O refresh também renova as claims: é aqui que uma mudança de role passa a valer.
  tokenStorage.setSession(data.dados)
  return data.dados.token
}

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as
      | (InternalAxiosRequestConfig & { _retry?: boolean })
      | undefined

    const url = original?.url ?? ''
    const isAnonymous = ANONYMOUS_ROUTES.some((rota) => url.includes(rota))
    if (error.response?.status !== 401 || !original || original._retry || isAnonymous) {
      throw error
    }

    original._retry = true
    try {
      refreshInFlight ??= refreshTokens().finally(() => {
        refreshInFlight = null
      })
      const newToken = await refreshInFlight
      original.headers.Authorization = `Bearer ${newToken}`
      return await http(original)
    } catch {
      tokenStorage.clear()
      window.location.assign('/login')
      throw error
    }
  },
)

/** Desembrulha o envelope da API, lançando erro amigável quando sucesso=false. */
export function unwrap<T>(response: ApiResponse<T>): T {
  if (!response.sucesso || response.dados === null) {
    throw new ApiError(response.mensagem, response.erros ?? [])
  }
  return response.dados
}

export class ApiError extends Error {
  erros: string[]

  constructor(message: string, erros: string[]) {
    super(message)
    this.name = 'ApiError'
    this.erros = erros
  }
}
