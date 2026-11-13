/** Colapsa espaços — inclusive o não separável que o Intl põe em "R$ 1,00". */
export const normalizar = (texto: string | null | undefined) => (texto ?? '').replace(/\s+/g, ' ').trim()

/**
 * Matcher para `getByText` que compara o texto **inteiro** do elemento, filhos incluídos —
 * "37 lançamentos" quando o número vem num `<strong>` separado. Fica com o elemento mais
 * interno que casa, para não devolver também todos os ancestrais.
 */
export function porTextoCompleto(esperado: string | RegExp) {
  const confere = (texto: string) => (typeof esperado === 'string' ? texto === esperado : esperado.test(texto))
  return (_: string, elemento: Element | null) => {
    if (!elemento || !confere(normalizar(elemento.textContent))) return false
    return Array.from(elemento.children).every((filho) => !confere(normalizar(filho.textContent)))
  }
}
