import type { ManutencaoResponse } from '../api/types'

/**
 * Dentro desta faixa a manutenção já aparece como "vencendo" — a mesma janela que a
 * LandingPage encena. Espelha `RegrasDeManutencao.FaixaAvisoKm` da API, que usa o mesmo
 * corte para contar as "vencendo" do `/manutencao/resumo`: mexeu numa, mexa na outra.
 */
export const FAIXA_AVISO = 500

/**
 * `atrasada` tem precedência sobre tudo: é o campo que a API recalcula a cada leitura
 * comparando o km previsto com a quilometragem atual do veículo (§7.3.3). A ordem dos
 * `if` é a regra — "vencendo" só existe para o que ainda não venceu.
 *
 * `Cancelada` não é produzida por nenhum endpoint hoje, mas o enum a prevê.
 *
 * Vive em `lib/` e não dentro da página, como `statusDaRota`: `/manutencoes` e
 * `/minhas-rotas` precisam nomear o mesmo estado do mesmo jeito.
 */
export function badgeDaManutencao(m: ManutencaoResponse): { rotulo: string; classe: string } {
  if (m.atrasada) return { rotulo: 'Atrasada', classe: 'tag tag-danger' }

  if (m.status === 'Pendente') {
    return estaVencendo(m)
      ? { rotulo: 'Vencendo', classe: 'tag tag-warning' }
      : { rotulo: 'Pendente', classe: 'tag tag-accent' }
  }

  if (m.status === 'Realizada') return { rotulo: 'Concluída', classe: 'tag tag-success' }
  return { rotulo: 'Cancelada', classe: 'tag tag-neutral' }
}

/**
 * Pendente, ainda não vencida, e dentro da faixa de aviso. Exposta à parte porque a
 * cor do texto de andamento e a borda do alerta de `/minhas-rotas` acompanham a tag.
 */
export function estaVencendo(m: ManutencaoResponse): boolean {
  return (
    !m.atrasada &&
    m.status === 'Pendente' &&
    m.kmRestantes != null &&
    m.kmRestantes <= FAIXA_AVISO
  )
}

/** `kmRestantes` vem negativo quando o veículo já passou do ponto, e null fora de "Pendente". */
export function textoKmRestantes(km: number | null | undefined): string | null {
  if (km == null) return null
  if (km < 0) return `${Math.abs(km).toLocaleString('pt-BR')} km em atraso`
  return `faltam ${km.toLocaleString('pt-BR')} km`
}
