import { AxiosError } from 'axios'
import { ApiError } from './http'
import type { ApiResponse } from './types'

/**
 * Extrai mensagens legíveis de qualquer falha da API. Como 400/401/403/404/422/429
 * vêm todos no mesmo envelope, `erros` alimenta formulários e `mensagem` (em português)
 * serve de fallback para toasts.
 */
export function mensagensDeErro(
  error: unknown,
  fallback = 'Ocorreu um erro. Tente novamente.',
): string[] {
  if (error instanceof ApiError) {
    return error.erros.length > 0 ? error.erros : [error.message]
  }

  if (error instanceof AxiosError) {
    const envelope = error.response?.data as ApiResponse<unknown> | undefined
    if (envelope && typeof envelope.mensagem === 'string') {
      return envelope.erros && envelope.erros.length > 0 ? envelope.erros : [envelope.mensagem]
    }
    if (!error.response) {
      return ['Não foi possível conectar à API. Verifique se ela está no ar.']
    }
  }

  return [fallback]
}
