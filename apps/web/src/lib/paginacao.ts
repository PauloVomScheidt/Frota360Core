import { useMemo, useState } from 'react'
import { gravarPreferencia, lerPreferencia } from './preferencias'

/**
 * Paginação das listagens do painel.
 *
 * **O corte é no cliente**, com duas exceções que já paginavam antes: `/auditoria` e
 * `/custos`, os únicos endpoints paginados da API. A escolha não é de economia de trabalho —
 * é que os rodapés de total de `/abastecimentos` e `/despesas` somam o **filtro inteiro**, e
 * paginar no servidor os reduziria à página visível. Enquanto a lista couber numa requisição,
 * fatiar aqui mantém aqueles números honestos de graça.
 */

export const TAMANHOS_PAGINA = [10, 15, 20] as const

export type TamanhoPagina = (typeof TAMANHOS_PAGINA)[number]

const CHAVE = 'frota360.itensPorPagina'
const PADRAO: TamanhoPagina = 15

function ehTamanhoValido(valor: number): valor is TamanhoPagina {
  return (TAMANHOS_PAGINA as readonly number[]).includes(valor)
}

/**
 * Quantos itens por página, lembrado entre telas e entre visitas. É uma preferência só para
 * o painel inteiro, não uma por tela: quem prefere 20 escolhe uma vez.
 *
 * Usado direto por `/auditoria` e `/custos`, que mandam o valor para a API e cuidam da
 * própria página. As demais telas pegam isto de dentro do `usePaginacao`.
 */
export function useTamanhoPagina() {
  const [tamanhoPagina, setEstado] = useState<TamanhoPagina>(() =>
    lerPreferencia(CHAVE, PADRAO, (bruto) => {
      const numero = Number(bruto)
      return ehTamanhoValido(numero) ? numero : null
    }),
  )

  function setTamanhoPagina(valor: TamanhoPagina) {
    setEstado(valor)
    gravarPreferencia(CHAVE, valor)
  }

  return { tamanhoPagina, setTamanhoPagina }
}

/**
 * O par `pagina` + `tamanhoPagina` de quem pagina **no servidor** — as quatro listas
 * transacionais, mais `/auditoria` e `/custos`. Devolve as props do componente `Paginacao`
 * já montadas a partir do `ResultadoPaginado` que a API respondeu.
 *
 * ⚠️ Diferente do `usePaginacao`, aqui **é obrigatório chamar `resetar()` a cada mudança de
 * filtro**: o clamp do cliente não alcança o que o servidor recortou, e sem o reset a tela
 * abre vazia ao filtrar estando na página 4.
 */
export function usePaginacaoServidor() {
  const { tamanhoPagina, setTamanhoPagina } = useTamanhoPagina()
  const [pagina, setPagina] = useState(1)

  function resetar() {
    setPagina(1)
  }

  /**
   * As props do rodapé. `dados` é o que a API devolveu — enquanto a primeira consulta não
   * volta, o rodapé some sozinho (total 0), que é o comportamento certo.
   */
  function props(dados: { pagina: number; totalPaginas: number; total: number; tamanhoPagina: number } | undefined) {
    return {
      pagina: dados?.pagina ?? pagina,
      totalPaginas: dados?.totalPaginas ?? 1,
      total: dados?.total ?? 0,
      tamanhoPagina: dados?.tamanhoPagina ?? tamanhoPagina,
      onMudar: setPagina,
      onMudarTamanho: (t: TamanhoPagina) => {
        setTamanhoPagina(t)
        resetar()
      },
    }
  }

  return { pagina, tamanhoPagina, resetar, props }
}

/**
 * Fatia uma lista **já filtrada** na página corrente. O retorno é o conjunto exato de props
 * do componente `Paginacao`, mais os itens — daí dar para espalhar:
 *
 * ```tsx
 * const p = usePaginacao(veiculosFiltrados)
 * {p.itensDaPagina.map(...)}
 * <Paginacao {...p} pending={query.isFetching} />
 * ```
 *
 * ⚠️ Passe a lista **depois** de todo filtro e ordenação do cliente. Paginar antes de filtrar
 * mostraria a página 1 de uma lista que não é a exibida.
 */
export function usePaginacao<T>(itens: T[]) {
  const { tamanhoPagina, setTamanhoPagina } = useTamanhoPagina()
  const [pagina, setPagina] = useState(1)

  const total = itens.length
  const totalPaginas = Math.max(1, Math.ceil(total / tamanhoPagina))

  /**
   * A página é clampada no render, e não corrigida por efeito: estando na página 5 quando um
   * filtro corta a lista para oito itens, a tabela abriria vazia. É o mesmo bug que as duas
   * telas paginadas no servidor evitam chamando `resetarPaginacao()` à mão em cada `onChange`
   * de filtro — aqui nenhuma tela precisa lembrar de nada, e não há estado a sincronizar.
   */
  const paginaValida = Math.min(pagina, totalPaginas)

  const itensDaPagina = useMemo(
    () => itens.slice((paginaValida - 1) * tamanhoPagina, paginaValida * tamanhoPagina),
    [itens, paginaValida, tamanhoPagina],
  )

  return {
    itensDaPagina,
    pagina: paginaValida,
    totalPaginas,
    total,
    tamanhoPagina,
    onMudar: setPagina,
    onMudarTamanho: setTamanhoPagina,
  }
}
