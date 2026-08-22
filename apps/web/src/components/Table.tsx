import type { FormEvent, ReactNode } from 'react'
import { mensagensDeErro } from '../api/errors'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

/** Painel de cadastro que abre acima da tabela (padrão "Novo X" do design). */
export function InlineForm({
  onSubmit,
  children,
}: {
  onSubmit: (e: FormEvent) => void
  children: ReactNode
}) {
  return (
    <form
      onSubmit={onSubmit}
      className="mb-8 flex flex-wrap items-end gap-4 p-5"
      style={{ border: '1px solid var(--color-divider)', background: 'var(--color-surface)' }}
    >
      {children}
    </form>
  )
}

/** Linha de carregando / erro / vazio dentro de um tbody. */
export function TableStates({
  colSpan,
  pending,
  error,
  empty,
  textoCarregando = 'Carregando…',
  textoErro,
  textoVazio,
}: {
  colSpan: number
  pending: boolean
  error: unknown
  empty: boolean
  textoCarregando?: string
  textoErro: string
  textoVazio: string
}) {
  if (pending) {
    return (
      <tr>
        <td colSpan={colSpan} style={{ color: mutedText }}>
          {textoCarregando}
        </td>
      </tr>
    )
  }

  if (error) {
    return (
      <tr>
        <td colSpan={colSpan} style={{ color: '#a03123' }}>
          {mensagensDeErro(error, textoErro)[0]}
        </td>
      </tr>
    )
  }

  if (empty) {
    return (
      <tr>
        <td colSpan={colSpan} style={{ color: mutedText }}>
          {textoVazio}
        </td>
      </tr>
    )
  }

  return null
}
