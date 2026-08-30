/**
 * Períodos prontos dos filtros de data, compartilhados por `/manutencoes` e
 * `/abastecimentos`.
 *
 * A API continua recebendo `de`/`ate` como sempre — a conversão é do cliente. Dois campos
 * de data soltos são o tipo de controle que exige do usuário aquilo que o sistema já sabe
 * fazer: quase toda consulta real é "os últimos dias" ou "este mês".
 */
export type Periodo = 'todos' | 'hoje' | '7dias' | '30dias' | 'esteMes' | 'mesPassado'

export const PERIODOS: { valor: Periodo; rotulo: string }[] = [
  { valor: 'todos', rotulo: 'Todo o período' },
  { valor: 'hoje', rotulo: 'Hoje' },
  { valor: '7dias', rotulo: 'Últimos 7 dias' },
  { valor: '30dias', rotulo: 'Últimos 30 dias' },
  { valor: 'esteMes', rotulo: 'Este mês' },
  { valor: 'mesPassado', rotulo: 'Mês passado' },
]

/** `Date` → `yyyy-MM-dd`, o formato que a API espera na query string. */
function paraIso(data: Date): string {
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
  }
}
