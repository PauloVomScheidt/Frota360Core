import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ResumoCustosResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import {
  fabricarCustoPorVeiculo,
  fabricarLancamentoCusto,
  fabricarResumoCustos,
  paginado,
} from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { CustosPage } from './CustosPage'

const RESUMO = fabricarResumoCustos({
  totalAbastecimento: 7200,
  totalManutencao: 3600,
  totalDespesa: 1200,
  quantidadeLancamentos: 37,
  kmTotal: 2000,
  custoPorKm: 0.4567,
  kmOdometroTotal: 3100,
  consumoMedio: 8.46,
  porVeiculo: [
    fabricarCustoPorVeiculo({ veiculoId: 1, veiculoPlaca: 'ABC1D01', veiculoNome: 'Caminhão 1', total: 9000, km: 1800 }),
    fabricarCustoPorVeiculo({
      veiculoId: 2,
      veiculoPlaca: 'ABC1D02',
      veiculoNome: 'Caminhão 2',
      totalAbastecimento: 0,
      totalManutencao: 0,
      totalDespesa: 0,
      total: 0,
      km: 0,
      custoPorKm: null,
      consumoMedio: null,
    }),
  ],
})

function montar(resumo: ResumoCustosResponse = RESUMO) {
  const resumos = capturar('get', '/custo/resumo', responderJson(resumo))
  return { ...renderizar(<CustosPage />, { rota: '/custos', papel: 'Supervisor' }), resumos }
}

/** O cartão do KPI — rótulo, valor e detalhe —, esperado até o resumo chegar. */
async function esperarKpi(rotulo: string, texto: string) {
  const cartao = (await within(screen.getByRole('main')).findByText(rotulo, { selector: 'span' })).parentElement!.parentElement!
  await waitFor(() => expect(cartao).toHaveTextContent(texto))
}

/** As opções dos filtros vêm de /veiculo e /motorista: escolher só depois que chegam. */
async function escolher(user: import('@testing-library/user-event').UserEvent, rotulo: string, opcao: string) {
  await screen.findByRole('option', { name: opcao })
  await user.selectOptions(screen.getByLabelText(rotulo), opcao)
}

const linha = (placa: string) => screen.findByRole('row', { name: new RegExp(placa) })

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date(2026, 10, 12, 10, 0))
})
afterEach(() => vi.useRealTimers())

describe('CustosPage — KPIs', () => {
  it('somam o que o servidor devolveu, com a participação de cada origem', async () => {
    montar()
    expect(screen.getByRole('heading', { name: 'Custos' })).toBeInTheDocument()
    await esperarKpi('Custo total', 'R$ 12.000,0037 lançamentos')
    await esperarKpi('Combustível', 'R$ 7.200,0060% do total')
    await esperarKpi('Manutenção', 'R$ 3.600,0030% do total')
    await esperarKpi('Despesas', 'R$ 1.200,0010% do total')
  })

  it('os dois km dizem de onde vêm: rotas encerradas e odômetro', async () => {
    montar()
    await esperarKpi('Custo por km', 'R$ 0,4567/km2.000 km em rotas encerradas')
    await esperarKpi('Consumo médio', '8,5 km/l3.100 km pelo odômetro')
  })

  it('período sem gasto e sem rota: participação explícita e R$/km "—", nunca zero', async () => {
    montar(
      fabricarResumoCustos({
        totalAbastecimento: 0,
        totalManutencao: 0,
        totalDespesa: 0,
        quantidadeLancamentos: 0,
        custoPorKm: null,
        consumoMedio: null,
        porVeiculo: [],
      }),
    )
    await esperarKpi('Combustível', 'sem gasto no período')
    await esperarKpi('Custo por km', '—')
    await esperarKpi('Consumo médio', '—')
    expect(await screen.findByText('Nenhum custo lançado no período escolhido.')).toBeInTheDocument()
  })

  it('abre no mês corrente', async () => {
    const { resumos } = montar()
    await esperarKpi('Custo total', 'lançamentos')
    expect(resumos[0].params).toEqual({ de: '2026-11-01', ate: '2026-11-30' })
  })

  it('falha ao somar aparece na tabela', async () => {
    capturar('get', '/custo/resumo', responderErro(500, ['Erro interno.']))
    renderizar(<CustosPage />, { papel: 'Admin' })
    expect(await screen.findByText(/Erro interno\.|Não foi possível carregar o resumo\./)).toBeInTheDocument()
  })
})

describe('CustosPage — por veículo', () => {
  it('lista cada veículo, inclusive o que rodou sem custo, com "—" no que não tem métrica', async () => {
    montar()
    expect(within(await linha('ABC1D01')).getAllByRole('cell')[4]).toHaveTextContent('R$ 9.000,00')
    const semCusto = within(await linha('ABC1D02')).getAllByRole('cell')
    expect(semCusto[4]).toHaveTextContent('R$ 0,00')
    expect(semCusto[5]).toHaveTextContent('—')
    expect(semCusto[6]).toHaveTextContent('—')
  })

  it('"Ver" abre os lançamentos do veículo com o recorte da tela, e "Fechar" fecha', async () => {
    const lancamentos = capturar(
      'get',
      '/custo',
      responderJson(paginado([fabricarLancamentoCusto({ categoria: 'Combustível', valor: 650 }), fabricarLancamentoCusto({ origem: 'Despesa', categoria: 'Pedágio', valor: 25.4, motoristaNome: null })])),
    )
    const { user } = montar()
    await user.click(within(await linha('ABC1D01')).getByRole('button', { name: 'Ver' }))

    const dialogo = screen.getByRole('dialog', { name: 'Lançamentos — ABC1D01' })
    expect(await within(dialogo).findByRole('row', { name: /Pedágio/ })).toHaveTextContent('Despesa')
    expect(lancamentos[0].params).toMatchObject({ veiculoId: '1', de: '2026-11-01', ate: '2026-11-30', pagina: '1' })

    await user.click(within(dialogo).getByRole('button', { name: 'Fechar' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('veículo sem lançamento no recorte diz isso no detalhe', async () => {
    capturar('get', '/custo', responderJson(paginado([])))
    const { user } = montar()
    await user.click(within(await linha('ABC1D02')).getByRole('button', { name: 'Ver' }))
    expect(await within(screen.getByRole('dialog')).findByText('Nenhum custo para os filtros escolhidos.')).toBeInTheDocument()
  })
})

describe('CustosPage — filtros e avisos', () => {
  it('filtrar por motorista vai para o servidor e acende os dois avisos', async () => {
    const { user, resumos } = montar()
    await esperarKpi('Custo total', 'lançamentos')
    await escolher(user, 'Motorista', 'João Pereira')

    await waitFor(() => expect(resumos.at(-1)?.params).toMatchObject({ motoristaId: '10' }))
    expect(screen.getByText(/Manutenção não é atribuída a motorista/)).toBeInTheDocument()
    expect(screen.getByText(/O consumo fica superestimado por motorista/)).toBeInTheDocument()
  })

  it('mudar o filtro fecha o detalhe aberto', async () => {
    capturar('get', '/custo', responderJson(paginado([])))
    const { user } = montar()
    await user.click(within(await linha('ABC1D01')).getByRole('button', { name: 'Ver' }))
    expect(screen.getByRole('dialog')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Origem'), 'Manutenção')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it.each([
    [1, '1 manutenção concluída sem custo informado não entra neste total.'],
    [3, '3 manutenções concluídas sem custo informado não entram neste total.'],
  ])('%s manutenção(ões) sem custo: avisa que o total não as inclui', async (quantidade, texto) => {
    montar(fabricarResumoCustos({ manutencoesSemCustoInformado: quantidade }))
    expect(await screen.findByText(texto)).toBeInTheDocument()
  })

  it('"Limpar filtros" e "Atualizar" refazem a soma', async () => {
    const { user, resumos } = montar()
    await esperarKpi('Custo total', 'lançamentos')
    await escolher(user, 'Veículo', 'ABC1D02 — Caminhão 2')
    await waitFor(() => expect(resumos.at(-1)?.params.veiculoId).toBe('2'))

    await user.click(screen.getByRole('button', { name: 'Limpar filtros' }))
    await waitFor(() => expect(resumos.at(-1)?.params).toEqual({}))

    const antes = resumos.length
    await user.click(screen.getByRole('button', { name: 'Atualizar' }))
    await waitFor(() => expect(resumos.length).toBe(antes + 1))
  })
})
