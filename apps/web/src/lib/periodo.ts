/**
 * Períodos dos filtros de data (`FiltroPeriodo`), compartilhados pelas telas de lançamento,
 * `/custos` e `/auditoria`.
 *
 * A API continua recebendo `de`/`ate` como sempre — a conversão é do cliente. As opções
 * prontas vêm primeiro porque quase toda consulta real é "os últimos dias" ou "este mês";
 * o intervalo livre fica por último, em "Personalizado…".
 */
export type Periodo =
  | 'todos'
  | 'hoje'
  | '7dias'
  | '30dias'
  | 'esteMes'
  | 'mesPassado'
  | '3meses'
  | '6meses'
  | 'esteAno'
  | 'anoPassado'
  | 'personalizado'

export const PERIODOS: { valor: Periodo; rotulo: string }[] = [
  { valor: 'todos', rotulo: 'Todo o período' },
  { valor: 'hoje', rotulo: 'Hoje' },
  { valor: '7dias', rotulo: 'Últimos 7 dias' },
  { valor: '30dias', rotulo: 'Últimos 30 dias' },
  { valor: 'esteMes', rotulo: 'Este mês' },
  { valor: 'mesPassado', rotulo: 'Mês passado' },
  { valor: '3meses', rotulo: 'Últimos 3 meses' },
  { valor: '6meses', rotulo: 'Últimos 6 meses' },
  { valor: 'esteAno', rotulo: 'Este ano' },
  { valor: 'anoPassado', rotulo: 'Ano passado' },
  { valor: 'personalizado', rotulo: 'Personalizado…' },
]

/** `de`/`ate` (aaaa-mm-dd) só valem em `personalizado`; nos demais o intervalo sai do período. */
export interface FiltroDePeriodo {
  periodo: Periodo
  de: string
  ate: string
}

export function filtroDePeriodo(periodo: Periodo): FiltroDePeriodo {
  return { periodo, de: '', ate: '' }
}

/** O intervalo que vai para a API. Personalizado aceita só um dos lados ("a partir de", "até"). */
export function intervaloDoFiltro(filtro: FiltroDePeriodo): { de?: string; ate?: string } {
  if (filtro.periodo !== 'personalizado') return intervaloDoPeriodo(filtro.periodo)
  return { de: filtro.de || undefined, ate: filtro.ate || undefined }
}

/** Datas em aaaa-mm-dd comparam como texto na ordem do calendário. */
export function erroDoIntervalo(de: string, ate: string): string | null {
  return de && ate && ate < de ? 'A data final não pode ser anterior à inicial.' : null
}

/** `Date` → `yyyy-MM-dd`, o formato que a API espera na query string. */
export function paraIso(data: Date): string {
  const mes = `${data.getMonth() + 1}`.padStart(2, '0')
  const dia = `${data.getDate()}`.padStart(2, '0')
  return `${data.getFullYear()}-${mes}-${dia}`
}

/**
 * Converte o período escolhido no intervalo que vai para a API. `ate` é inclusivo do lado
 * do servidor (ele estende até o fim do dia), então "hoje" manda o mesmo dia nos dois.
 *
 * Datas em hora local de propósito: o usuário escolhe "hoje" pensando no calendário dele,
 * não em UTC.
 */
export function intervaloDoPeriodo(periodo: Periodo): { de?: string; ate?: string } {
  const hoje = new Date()

  switch (periodo) {
    case 'todos':
      return {}

    case 'hoje':
      return { de: paraIso(hoje), ate: paraIso(hoje) }

    // Inclui o dia de hoje na contagem: "últimos 7 dias" são hoje e os 6 anteriores.
    case '7dias': {
      const inicio = new Date(hoje)
      inicio.setDate(inicio.getDate() - 6)
      return { de: paraIso(inicio), ate: paraIso(hoje) }
    }

    case '30dias': {
      const inicio = new Date(hoje)
      inicio.setDate(inicio.getDate() - 29)
      return { de: paraIso(inicio), ate: paraIso(hoje) }
    }

    case 'esteMes': {
      const inicio = new Date(hoje.getFullYear(), hoje.getMonth(), 1)
      // Sem `ate`: em manutenções o período também alcança pendências futuras deste mês,
      // e cortar em "hoje" esconderia justamente o que ainda vai vencer.
      const fim = new Date(hoje.getFullYear(), hoje.getMonth() + 1, 0)
      return { de: paraIso(inicio), ate: paraIso(fim) }
    }

    case 'mesPassado': {
      const inicio = new Date(hoje.getFullYear(), hoje.getMonth() - 1, 1)
      // Dia 0 do mês seguinte é o último dia do mês anterior — e o construtor
      // normaliza a virada de ano sozinho.
      const fim = new Date(hoje.getFullYear(), hoje.getMonth(), 0)
      return { de: paraIso(inicio), ate: paraIso(fim) }
    }

    // Os meses cheios anteriores mais o corrente, que vai até o fim — mesmo motivo do
    // `esteMes`: em manutenções, cortar em hoje esconderia o que ainda vai vencer.
    case '3meses':
    case '6meses': {
      const meses = periodo === '3meses' ? 3 : 6
      const inicio = new Date(hoje.getFullYear(), hoje.getMonth() - (meses - 1), 1)
      const fim = new Date(hoje.getFullYear(), hoje.getMonth() + 1, 0)
      return { de: paraIso(inicio), ate: paraIso(fim) }
    }

    case 'esteAno':
      return { de: paraIso(new Date(hoje.getFullYear(), 0, 1)), ate: paraIso(new Date(hoje.getFullYear(), 11, 31)) }

    case 'anoPassado':
      return {
        de: paraIso(new Date(hoje.getFullYear() - 1, 0, 1)),
        ate: paraIso(new Date(hoje.getFullYear() - 1, 11, 31)),
      }

    // Sem as datas, que vivem no `FiltroDePeriodo` — use `intervaloDoFiltro`.
    case 'personalizado':
      return {}
  }
}

export interface Intervalo {
  de: string
  ate: string
}

/** Do dia 1 até hoje — o mês corrente sem os dias que ainda não aconteceram. */
export function mesAteHoje(hoje = new Date()): Intervalo {
  return { de: paraIso(new Date(hoje.getFullYear(), hoje.getMonth(), 1)), ate: paraIso(hoje) }
}

/**
 * Do dia 1 ao mesmo dia do mês anterior, para comparar com `mesAteHoje` sem pôr um mês
 * fechado contra um mês pela metade. Em 31/03 o fim é 28 ou 29/02, não 03/03.
 */
export function mesmoPeriodoDoMesPassado(hoje = new Date()): Intervalo {
  const ultimoDia = new Date(hoje.getFullYear(), hoje.getMonth(), 0).getDate()
  return {
    de: paraIso(new Date(hoje.getFullYear(), hoje.getMonth() - 1, 1)),
    ate: paraIso(new Date(hoje.getFullYear(), hoje.getMonth() - 1, Math.min(hoje.getDate(), ultimoDia))),
  }
}

/** Os últimos `quantidade` meses, o corrente incluído até hoje. */
export function ultimosMeses(quantidade: number, hoje = new Date()): Intervalo {
  return {
    de: paraIso(new Date(hoje.getFullYear(), hoje.getMonth() - (quantidade - 1), 1)),
    ate: paraIso(hoje),
  }
}
