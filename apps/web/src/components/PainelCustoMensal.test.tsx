import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { CustoPorMesResponse } from '../api/types'
import { porTextoCompleto } from '../test/consultas'
import { fabricarCustoPorMes } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { PainelCustoMensal, type ComparacaoParcial } from './PainelCustoMensal'

const VAZIO = { totalAbastecimento: 0, totalManutencao: 0, totalDespesa: 0 }

/** jun → nov/2026; julho sem lançamento. */
const SEMESTRE: CustoPorMesResponse[] = [
  fabricarCustoPorMes(2026, 6, { totalAbastecimento: 600, totalManutencao: 300, totalDespesa: 100 }),
  fabricarCustoPorMes(2026, 7, VAZIO),
  fabricarCustoPorMes(2026, 8, { totalAbastecimento: 1200, totalManutencao: 600, totalDespesa: 200 }),
  fabricarCustoPorMes(2026, 9, { totalAbastecimento: 1800, totalManutencao: 900, totalDespesa: 300 }),
  fabricarCustoPorMes(2026, 10, { totalAbastecimento: 1200, totalManutencao: 600, totalDespesa: 200 }),
  fabricarCustoPorMes(2026, 11, { totalAbastecimento: 1500, totalManutencao: 0, totalDespesa: 0 }),
]

const PARCIAL: ComparacaoParcial = {
  total: 1000,
  atual: { de: '2026-11-01', ate: '2026-11-12' },
  anterior: { de: '2026-10-01', ate: '2026-10-12' },
}

function painel(meses = SEMESTRE, comparacaoParcial: ComparacaoParcial | null = PARCIAL) {
  return renderizar(
    <PainelCustoMensal
      titulo="Custo dos últimos 6 meses"
      meses={meses}
      comparacaoParcial={comparacaoParcial ?? undefined}
      textoVazio="Nada lançado ainda."
    />,
  )
}

const coluna = (rotulo: RegExp) => screen.getByRole('button', { name: rotulo })
const resumo = () => screen.getByRole('complementary')

describe('PainelCustoMensal', () => {
  it('sem custo em nenhum mês, mostra só o texto', () => {
    painel(SEMESTRE.map((m) => fabricarCustoPorMes(m.ano, m.mes, VAZIO)))
    expect(screen.getByText('Nada lançado ainda.')).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('uma coluna por mês, nomeando valor ou "sem registros"', () => {
    painel()
    expect(screen.getAllByRole('button')).toHaveLength(6)
    expect(coluna(/^Junho de 2026: R\$\s1\.000,00$/)).toBeInTheDocument()
    expect(coluna(/^Julho de 2026: sem registros$/)).toBeInTheDocument()
  })

  it('começa no último mês, comparado com o mesmo trecho do mês anterior', () => {
    painel()
    expect(coluna(/^Novembro de 2026/)).toHaveAttribute('aria-pressed', 'true')
    expect(resumo()).toHaveTextContent('Novembro de 2026')
    expect(resumo()).toHaveTextContent('+50%vs. 1 a 12/10')
    expect(resumo()).toHaveTextContent('Mês em andamento: compara 1 a 12/11 com 1 a 12/10.')
  })

  it('a composição do mês mostra cada série com valor e participação', () => {
    painel()
    expect(screen.getByText(porTextoCompleto(/^Combustível\s*R\$ 1\.500,00\s*100%$/))).toBeInTheDocument()
    expect(screen.getByText(porTextoCompleto(/^Manutenção\s*R\$ 0,00\s*0%$/))).toBeInTheDocument()
  })

  it.each([
    [/^Setembro/, '+50%vs. ago/26'],
    [/^Outubro/, '−33,3%vs. set/26'],
  ])('mês fechado (%s) compara com o anterior da lista', async (mes, esperado) => {
    const { user } = painel()
    await user.click(coluna(mes))
    expect(resumo()).toHaveTextContent(esperado)
  })

  it.each([
    [/^Junho/, 'Primeiro mês do período.'],
    [/^Agosto/, 'Sem registros em jul/26 para comparar.'],
    [/^Julho/, 'Nenhum custo lançado neste mês.'],
  ])('%s explica por que não há variação', async (mes, nota) => {
    const { user } = painel()
    await user.click(coluna(mes))
    expect(resumo()).toHaveTextContent(nota)
  })

  it('mês corrente sem base no mês anterior diz isso', () => {
    painel(SEMESTRE, { ...PARCIAL, total: 0 })
    expect(resumo()).toHaveTextContent('Sem registros de 1 a 12/10 para comparar.')
  })

  it('sem comparação parcial, o mês corrente compara com o mês anterior inteiro', () => {
    painel(SEMESTRE, null)
    expect(resumo()).toHaveTextContent('−25%vs. out/26')
  })

  it('variação zero fica sem sinal', async () => {
    const { user } = painel(SEMESTRE, { ...PARCIAL, total: 1500 })
    expect(resumo()).toHaveTextContent('0%vs. 1 a 12/10')
    await user.click(coluna(/^Junho/))
    expect(resumo()).toHaveTextContent('Junho de 2026')
  })

  it('com dois ou mais meses com lançamento, mostra a média como legenda', () => {
    painel()
    expect(screen.getByText(porTextoCompleto(/^Média R\$ 1,9 mil$/))).toBeInTheDocument()
  })

  it('focar uma coluna pelo teclado também seleciona o mês', async () => {
    const { user } = painel()
    await user.tab()
    expect(coluna(/^Junho/)).toHaveAttribute('aria-pressed', 'true')
    expect(resumo()).toHaveTextContent('Junho de 2026')
  })

  it('passar o mouse numa fatia abre o tooltip dela, e sair do gráfico o fecha', async () => {
    const { user } = painel()
    const setembro = coluna(/^Setembro/)
    // A pilha é o último filho da coluna; cada fatia é um span sem papel, de baixo para cima.
    const [combustivel] = Array.from(setembro.lastElementChild!.children)

    await user.hover(combustivel)

    const tooltip = screen.getByRole('tooltip')
    expect(tooltip).toHaveTextContent('Setembro de 2026')
    expect(tooltip).toHaveTextContent('R$ 1.800,00')
    expect(tooltip).toHaveTextContent('Combustível · 60% do mês')
    expect(tooltip).toHaveTextContent('Total do mêsR$ 3.000,00')

    await user.unhover(setembro.parentElement!.parentElement!)
    expect(screen.queryByRole('tooltip')).not.toBeInTheDocument()
  })
})
