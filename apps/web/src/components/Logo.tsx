/**
 * Marca do Frota 360 — caminhão geométrico do design system.
 * As três cores são parametrizadas para funcionar tanto sobre o fundo claro
 * quanto sobre o painel azul-marinho.
 */
type Tom = 'ink' | 'light'

const CORES: Record<Tom, { body: string; stripe: string; cutout: string }> = {
  ink: { body: '#201e1d', stripe: '#1f3a5f', cutout: '#fdfaf6' },
  light: { body: '#fdfaf6', stripe: '#5c7896', cutout: '#0c1a2a' },
}

export function LogoMark({ size = 30, tom = 'ink' }: { size?: number; tom?: Tom }) {
  const { body, stripe, cutout } = CORES[tom]
  return (
    <svg
      width={size}
      height={size * (44 / 64)}
      viewBox="0 0 64 44"
      style={{ display: 'block', flex: 'none' }}
      role="img"
      aria-label="Frota 360"
    >
      <line x1={2} y1={36} x2={62} y2={36} stroke={body} strokeWidth={2.5} />
      <rect x={6} y={10} width={30} height={20} fill={body} />
      <rect x={6} y={17} width={30} height={5} fill={stripe} />
      <rect x={36} y={16} width={14} height={14} fill={body} />
      <rect x={39} y={19} width={6} height={6} fill={cutout} />
      <circle cx={16} cy={36} r={5} fill={body} />
      <circle cx={16} cy={36} r={2} fill={cutout} />
      <circle cx={42} cy={36} r={5} fill={body} />
      <circle cx={42} cy={36} r={2} fill={cutout} />
    </svg>
  )
}

/** "Frota 360" com o número em destaque. */
export function Wordmark({
  size = 20,
  cor = 'var(--color-text)',
  corDestaque = 'var(--color-accent)',
}: {
  size?: number
  cor?: string
  corDestaque?: string
}) {
  return (
    <span
      style={{
        fontFamily: 'var(--font-heading)',
        fontWeight: 800,
        fontSize: size,
        letterSpacing: '-0.02em',
        lineHeight: 1,
        color: cor,
        whiteSpace: 'nowrap',
      }}
    >
      Frota <span style={{ color: corDestaque }}>360</span>
    </span>
  )
}
