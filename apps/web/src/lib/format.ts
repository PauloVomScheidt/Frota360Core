/** Data ISO → dd/mm/aaaa. */
export function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString('pt-BR')
}

/** Data ISO → dd/mm/aaaa hh:mm. */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString('pt-BR')
}

/** Data ISO (com ou sem hora) → aaaa-mm-dd, o formato aceito por `<input type="date">`. */
export function paraInputDate(iso: string | null | undefined): string {
  if (!iso) return ''
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  const mes = `${date.getMonth() + 1}`.padStart(2, '0')
  const dia = `${date.getDate()}`.padStart(2, '0')
  return `${date.getFullYear()}-${mes}-${dia}`
}

export function formatKm(km: number): string {
  return `${km.toLocaleString('pt-BR')} km`
}

/** Volume da bomba: até 3 casas, sem zeros à direita inúteis (48,5 e não 48,500). */
export function formatLitros(litros: number): string {
  return litros.toLocaleString('pt-BR', { maximumFractionDigits: 3 })
}

/**
 * km/l com uma casa. Duas dariam falsa precisão a um número que já é estimativa —
 * abastecimento parcial e veículo flex distorcem a conta.
 */
export function formatConsumo(kmPorLitro: number): string {
  return `${kmPorLitro.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })} km/l`
}

/** Número → R$ 1.234,50. Custo é opcional na manutenção, daí o traço. */
export function formatMoeda(valor: number | null | undefined): string {
  if (valor === null || valor === undefined) return '—'
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

/** Hoje em aaaa-mm-dd, para pré-preencher `<input type="date">`. */
export function hojeInputDate(): string {
  return paraInputDate(new Date().toISOString())
}

/** 12345678901 → 123.456.789-01 (a API guarda só os 11 dígitos). */
export function formatCpf(cpf: string): string {
  const digitos = somenteDigitos(cpf)
  if (digitos.length !== 11) return cpf
  return `${digitos.slice(0, 3)}.${digitos.slice(3, 6)}.${digitos.slice(6, 9)}-${digitos.slice(9)}`
}

export function somenteDigitos(valor: string): string {
  return valor.replace(/\D/g, '')
}

/** Máscara progressiva enquanto o usuário digita o CPF. */
export function mascaraCpf(valor: string): string {
  const d = somenteDigitos(valor).slice(0, 11)
  if (d.length <= 3) return d
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
}

/** Iniciais para o avatar. */
export function iniciais(nome: string): string {
  const partes = nome.trim().split(/\s+/)
  const primeira = partes[0]?.[0] ?? ''
  const ultima = partes.length > 1 ? (partes[partes.length - 1][0] ?? '') : ''
  return (primeira + ultima).toUpperCase() || '??'
}
