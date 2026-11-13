import type { RotaResponse } from '../api/types'

/**
 * A API não tem campo de status: derivamos de `ativo` + `dataFim`, que é o que
 * existe hoje (a RotaResponse é flat — ver §16 do CONTEXTO).
 *
 * Compartilhado entre `/rotas` (gestão) e `/minhas-rotas` (motorista) para que as
 * duas telas nomeiem o mesmo estado do mesmo jeito.
 */
export function statusDaRota(rota: RotaResponse): { rotulo: string; classe: string } {
  if (rota.ativo) return { rotulo: 'Ativa', classe: 'tag tag-accent' }
  // Encerrada é conclusão bem-sucedida (verde); inativa é ausência de estado (cinza).
  // As duas eram neutras e ficavam indistinguíveis na tabela.
  if (rota.dataFim) return { rotulo: 'Encerrada', classe: 'tag tag-success' }
  return { rotulo: 'Inativa', classe: 'tag tag-neutral' }
}

/**
 * A partir de quantos dias uma rota aberta vira alerta no dashboard. Corte de leitura, não
 * regra da API: viagem que atravessa a madrugada é normal, dois dias costuma ser rota que
 * ninguém encerrou — e ela segura o odômetro e infla o R$/km do mês.
 */
export const DIAS_ALERTA_ROTA_ABERTA = 2

/** Dias de calendário entre o início da rota e hoje — `dataInicio` costuma vir sem hora. */
export function diasEmAberto(rota: RotaResponse, hoje = new Date()): number {
  const inicio = new Date(rota.dataInicio)
  const diaInicio = new Date(inicio.getFullYear(), inicio.getMonth(), inicio.getDate())
  const diaHoje = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate())
  return Math.round((diaHoje.getTime() - diaInicio.getTime()) / 86_400_000)
}
