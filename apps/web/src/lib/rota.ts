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
