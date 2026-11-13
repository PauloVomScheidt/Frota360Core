/**
 * Preferências de interface no `localStorage` — o que é conveniência de quem usa, não dado
 * do sistema: a sidebar retraída, os itens por página. Nada aqui viaja para a API nem é
 * fonte de verdade de coisa alguma.
 *
 * Todo acesso é embrulhado em `try/catch` porque o storage pode simplesmente lançar (aba
 * anônima, cookies de site bloqueados). Falhando, a tela cai no padrão e segue — perder a
 * preferência é irrelevante, quebrar o render não é.
 */

export function lerPreferencia<T>(chave: string, padrao: T, decodificar: (bruto: string) => T | null): T {
  try {
    const bruto = localStorage.getItem(chave)
    if (bruto === null) return padrao
    return decodificar(bruto) ?? padrao
  } catch {
    return padrao
  }
}

export function gravarPreferencia(chave: string, valor: string | number | boolean) {
  try {
    localStorage.setItem(chave, String(valor))
  } catch {
    // Preferência é conveniência: se o storage falhar, segue sem persistir.
  }
}

/** O caso mais comum, com o decodificador pronto. */
export function lerPreferenciaBooleana(chave: string, padrao: boolean): boolean {
  return lerPreferencia(chave, padrao, (bruto) => bruto === 'true')
}
