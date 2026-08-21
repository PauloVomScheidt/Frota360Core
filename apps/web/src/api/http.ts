import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { tokenStorage } from './tokenStorage'
import type { ApiResponse, AuthResponse } from './types'

export const http = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
})

http.interceptors.request.use((config) => {
  const token = tokenStorage.getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let refreshInFlight: Promise<string> | null = null

async function refreshTokens(): Promise<string> {
  const refreshToken = tokenStorage.getRefreshToken()
  if (!refreshToken) throw new Error('Sem refresh token')

  // axios "cru" para não passar pelos interceptors deste cliente
  const { data } = await axios.post<ApiResponse<AuthResponse>>(
    `${import.meta.env.VITE_API_URL}/auth/refresh`,
    { refreshToken },
  )
  if (!data.dados) throw new Error('Refresh sem dados')

  tokenStorage.set(data.dados.token, data.dados.refreshToken)
  return data.dados.token
}

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as
      | (InternalAxiosRequestConfig & { _retry?: boolean })
      | undefined

    const isAuthRoute = original?.url?.includes('/auth/')
    if (error.response?.status !== 401 || !original || original._retry || isAuthRoute) {
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
