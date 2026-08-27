import type { FormEvent, ReactNode } from 'react'

/** Container centralizado das telas de autenticação secundárias (convite, reset, esqueci). */
export function AuthScreen({
  largura = 380,
  onSubmit,
  children,
}: {
  largura?: number
  onSubmit?: (e: FormEvent) => void
  children: ReactNode
}) {
  return (
    <div className="flex min-h-screen items-center justify-center p-10">
      <form onSubmit={onSubmit} className="flex w-full flex-col gap-5" style={{ maxWidth: largura }}>
        {children}
      </form>
    </div>
  )
}

export function AuthHeading({
  kicker,
  titulo,
  descricao,
}: {
  kicker: string
  titulo: string
  descricao?: ReactNode
}) {
  return (
    <div>
      <div
        className="mb-2 text-[11px] uppercase"
        style={{ letterSpacing: '0.1em', color: 'var(--color-accent-700)' }}
      >
        {kicker}
      </div>
      <h2 style={{ margin: '0 0 8px' }}>{titulo}</h2>
      {descricao && (
        <p
          className="m-0 text-[13px]"
          style={{ color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}
        >
          {descricao}
        </p>
      )}
    </div>
  )
}
