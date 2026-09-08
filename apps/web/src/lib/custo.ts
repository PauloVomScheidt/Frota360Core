import type { OrigemCusto } from '../api/types'

export const ROTULO_ORIGEM: Record<OrigemCusto, string> = {
  Abastecimento: 'Abastecimento',
  Manutencao: 'Manutenção',
  Despesa: 'Despesa',
}

const MESES = [
  'jan',
  'fev',
  'mar',
  'abr',
  'mai',
  'jun',
  'jul',
  'ago',
  'set',
  'out',
  'nov',
  'dez',
]

/** `{ ano: 2026, mes: 8 }` → "ago/26". Rótulo curto porque cabe sob a barra do gráfico. */
export function rotuloDoMes(ano: number, mes: number): string {
  return `${MESES[mes - 1] ?? '?'}/${`${ano}`.slice(-2)}`
}

/**
 * R$/km com quatro casas — o valor costuma ficar abaixo de um real, e duas casas
 * transformariam a diferença entre veículos em "0,50" para todo mundo.
 *
 * Nulo quando não houve rota encerrada no período: sem denominador não existe métrica, e
 * mostrar zero afirmaria que a frota rodou de graça.
 */
export function formatCustoPorKm(valor: number | null | undefined): string {
  if (valor === null || valor === undefined) return '—'
  return `${valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 2, maximumFractionDigits: 4 })}/km`
}

/**
 * km/l com uma casa. Duas dariam falsa precisão a um número que já é estimativa:
 * abastecimento parcial, veículo flex e não-combustível no catálogo distorcem a conta.
 *
 * Nulo quando o veículo teve menos de dois abastecimentos no período — sem intervalo não
 * existe consumo, e mostrar zero afirmaria que ele rodou sem gastar.
 */
export function formatConsumo(valor: number | null | undefined): string {
  if (valor === null || valor === undefined) return '—'
  return `${valor.toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })} km/l`
}
