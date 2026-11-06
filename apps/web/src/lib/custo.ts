import type { CustoPorMesResponse, OrigemCusto } from '../api/types'
import type { Intervalo } from './periodo'

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
/**
 * Um item por mês do intervalo, na ordem, zerando os que a API omitiu — `porMes` só traz
 * mês com lançamento, e um gráfico de período fixo precisa das colunas vazias também.
 */
export function preencherMeses(meses: CustoPorMesResponse[], intervalo: Intervalo): CustoPorMesResponse[] {
  const [anoInicio, mesInicio] = intervalo.de.split('-').map(Number)
  const [anoFim, mesFim] = intervalo.ate.split('-').map(Number)
  const porChave = new Map(meses.map((m) => [`${m.ano}-${m.mes}`, m]))
  const resultado: CustoPorMesResponse[] = []

  for (let ano = anoInicio, mes = mesInicio; ano < anoFim || (ano === anoFim && mes <= mesFim); ) {
    resultado.push(
      porChave.get(`${ano}-${mes}`) ?? {
        ano,
        mes,
        totalAbastecimento: 0,
        totalManutencao: 0,
        totalDespesa: 0,
        total: 0,
      },
    )
    mes += 1
    if (mes > 12) {
      mes = 1
      ano += 1
    }
  }

  return resultado
}

/**
 * Escala do painel mensal: sempre três marcas (0, passo, 2 × passo), com o passo "limpo" —
 * 1, 2, 2,5 ou 5 × 10ⁿ, o que dá 2,5 mil / 5 mil / 10 mil nas faixas comuns — e o teto sempre
 * acima do maior valor, para a coluna mais alta nunca encostar no topo.
 */
export function escalaDeTresMarcas(maximo: number): { teto: number; marcas: number[] } {
  if (maximo <= 0) return { teto: 1, marcas: [0] }
  const bruto = maximo / 2
  const magnitude = 10 ** Math.floor(Math.log10(bruto))
  let passo = ([1, 2, 2.5, 5, 10].find((f) => f * magnitude >= bruto) ?? 10) * magnitude
  if (passo * 2 <= maximo) passo = ([2, 2.5, 5, 10].find((f) => f * magnitude > passo) ?? 20) * magnitude
  return { teto: passo * 2, marcas: [0, passo, passo * 2] }
}

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

export type SerieCusto = 'abastecimento' | 'manutencao' | 'despesa'

/**
 * Degraus 500/400/300 da rampa do acento: são categorias, não situações. Passam na separação
 * para daltonismo (ΔE ≥ 18); o 300 fica abaixo de 3:1 contra a superfície, e por isso a
 * identidade nunca depende só da cor — legenda, tooltip e a tabela para leitor de tela.
 * A ordem é a do empilhamento, de baixo para cima.
 */
export const SERIES_CUSTO: { chave: SerieCusto; rotulo: string; cor: string; valor: (m: CustoPorMesResponse) => number }[] = [
  { chave: 'abastecimento', rotulo: 'Combustível', cor: 'var(--color-accent-500)', valor: (m) => m.totalAbastecimento },
  { chave: 'manutencao', rotulo: 'Manutenção', cor: 'var(--color-accent-400)', valor: (m) => m.totalManutencao },
  { chave: 'despesa', rotulo: 'Despesas', cor: 'var(--color-accent-300)', valor: (m) => m.totalDespesa },
]

export function nomeDoMes(m: CustoPorMesResponse): string {
  const nome = new Date(m.ano, m.mes - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  return nome.charAt(0).toUpperCase() + nome.slice(1)
}

export function percentual(parte: number, total: number): string {
  return `${Math.round((parte / total) * 100)}%`
}
