import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ResumoCustosResponse, ResumoManutencoes, RotaResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { porTextoCompleto } from '../test/consultas'
import {
  fabricarCustoPorMes,
  fabricarCustoPorVeiculo,
  fabricarManutencao,
  fabricarResumoCustos,
  fabricarRota,
  fabricarVeiculo,
  paginado,
} from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { DashboardPage } from './DashboardPage'

// Hoje é 12/11/2026: o mês vai de 01 a 12/11, a comparação de 01 a 12/10 e o semestre de 01/06 a 12/11.
const MES = { de: '2026-11-01', ate: '2026-11-12' }
const COMPARACAO = { de: '2026-10-01', ate: '2026-10-12' }
const SEMESTRE = { de: '2026-06-01', ate: '2026-11-12' }

const CUSTO_DO_MES = fabricarResumoCustos({
  totalAbastecimento: 9000,
  totalManutencao: 2400,
  totalDespesa: 600,
  kmTotal: 2000,
  custoPorKm: 0.4567,
  kmOdometroTotal: 3100,
  consumoMedio: 8.46,
  manutencoesSemCustoInformado: 2,
  porVeiculo: [
    fabricarCustoPorVeiculo({ veiculoId: 1, veiculoPlaca: 'ABC1D01', total: 8000, custoPorKm: 0.5 }),
    fabricarCustoPorVeiculo({ veiculoId: 2, veiculoPlaca: 'ABC1D02', total: 4000 }),
    // Rodou no mês e ninguém lançou abastecimento: é pendência, e fica fora do ranking.
    fabricarCustoPorVeiculo({ veiculoId: 3, veiculoPlaca: 'ABC1D03', veiculoNome: 'Caminhão 3', totalAbastecimento: 0, totalManutencao: 0, totalDespesa: 0, total: 0, km: 450 }),
  ],
})
const SEMESTRE_RESUMO = fabricarResumoCustos({
  porMes: [fabricarCustoPorMes(2026, 9), fabricarCustoPorMes(2026, 11, { totalAbastecimento: 9000, totalManutencao: 2400, totalDespesa: 600 })],
})

const MANUTENCOES: ResumoManutencoes = {
  atrasadas: 2,
  vencendo: 3,
  urgentes: [
    fabricarManutencao({ id: 1, tipoManutencaoNome: 'Troca de óleo', veiculoPlaca: 'ABC1D01', atrasada: true, kmRestantes: -320 }),
    fabricarManutencao({ id: 2, tipoManutencaoNome: 'Revisão de freios', veiculoPlaca: 'ABC1D02', atrasada: true, kmRestantes: 800, dataPrevista: '2026-11-05T00:00:00' }),
    fabricarManutencao({ id: 3, tipoManutencaoNome: 'Alinhamento', veiculoPlaca: 'ABC1D02', atrasada: false, kmRestantes: 400 }),
  ],
}

const ROTAS = [
  fabricarRota({ id: 70, nomeMotorista: 'João Pereira', veiculoPlaca: 'ABC1D01', enderecoPartida: 'Joinville', enderecoChegada: 'Curitiba', dataInicio: '2026-11-09T00:00:00' }),
  fabricarRota({ id: 71, nomeMotorista: 'Carla Souza', veiculoPlaca: 'ABC1D02', enderecoPartida: 'Blumenau', enderecoChegada: 'Itajaí', dataInicio: '2026-11-12T00:00:00', dataCalculoRota: '2026-11-12T00:00:00', distanciaMetros: 47_000, duracaoEstimadaSegundos: 2_880 }),
]

interface Cenario {
  custoDoMes?: ResumoCustosResponse
  custoComparacao?: ResumoCustosResponse
  manutencoes?: ResumoManutencoes
  rotas?: RotaResponse[]
  totalRotas?: number
  manutencoesFalham?: boolean
}

function montar({
  custoDoMes = CUSTO_DO_MES,
  custoComparacao = fabricarResumoCustos({ totalAbastecimento: 7500, totalManutencao: 2000, totalDespesa: 500 }),
  manutencoes = MANUTENCOES,
  rotas = ROTAS,
  totalRotas = rotas.length,
  manutencoesFalham = false,
}: Cenario = {}) {
  capturar('get', '/veiculo', responderJson([fabricarVeiculo({ id: 1, emRota: true }), fabricarVeiculo({ id: 2, emRota: false })]))
  const rotasAtivas = capturar('get', '/rota', responderJson(paginado(rotas, 1, 100, totalRotas)))
  const kmDoMes = capturar('get', '/rota/resumo', responderJson({ quantidade: 4, kmTotal: 2000 }))
  const custos = capturar('get', '/custo/resumo', ({ params }) => {
    if (params.de === MES.de) return responderJson(custoDoMes)()
    if (params.de === COMPARACAO.de) return responderJson(custoComparacao)()
    return responderJson(SEMESTRE_RESUMO)()
  })
  const resumoManutencoes = capturar(
    'get',
    '/manutencao/resumo',
    manutencoesFalham ? responderErro(500, ['Erro interno.']) : responderJson(manutencoes),
  )
  const tela = renderizar(<DashboardPage />, { rota: '/dashboard', papel: 'Admin' })
  return { ...tela, rotasAtivas, kmDoMes, custos, resumoManutencoes }
}

async function esperarKpi(rotulo: string, texto: string | RegExp) {
  // A faixa de KPIs vem antes no DOM; "Manutenção" também é etiqueta de pendência e série do painel.
  const [rotuloDoKpi] = await within(screen.getByRole('main')).findAllByText(rotulo, { selector: 'span' })
  const cartao = rotuloDoKpi.parentElement!.parentElement!
  await waitFor(() => expect(cartao).toHaveTextContent(texto))
  return cartao
}

const atencao = () => screen.getByRole('region', { name: 'Precisa de atenção' })

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date(2026, 10, 12, 10, 0))
})
afterEach(() => vi.useRealTimers())

describe('DashboardPage — KPIs', () => {
  it('título, frota agora e manutenção', async () => {
    montar()
    expect(screen.getByRole('heading', { name: 'Visão geral da frota' })).toBeInTheDocument()
    await esperarKpi('Frota agora', '1 / 2em rota · 1 disponíveis')
    await esperarKpi('Manutenção', '2 atrasadas3 vencendo em até 500 km')
  })

  it('custo do mês compacto, com o valor cheio no title e a variação contra o mesmo trecho do mês passado', async () => {
    montar()
    const cartao = await esperarKpi('Custo do mês', 'R$ 12 mil+20% vs. 1 a 12 de outubro')
    expect(cartao).toHaveAttribute('title', expect.stringMatching(/R\$\s12\.000,00/))
  })

  it('sem custo no mesmo trecho do mês passado, não inventa variação', async () => {
    montar({ custoComparacao: fabricarResumoCustos({ totalAbastecimento: 0, totalManutencao: 0, totalDespesa: 0 }) })
    await esperarKpi('Custo do mês', 'nada lançado de 1 a 12 de outubro')
  })

  it('km rodado, R$/km e consumo, cada um dizendo de onde vem o km', async () => {
    montar()
    await esperarKpi('Km rodado', '2.000no mês · 4 rotas encerradas')
    await esperarKpi('Custo por km', 'R$ 0,45672.000 km em rotas encerradas')
    await esperarKpi('Consumo médio', '8,5 km/l3.100 km pelo odômetro')
  })

  it('pede os três recortes de custo e as rotas abertas', async () => {
    const { custos, rotasAtivas, kmDoMes } = montar()
    await esperarKpi('Custo do mês', 'R$ 12 mil')
    expect(custos.map((c) => c.params).sort((a, b) => a.de.localeCompare(b.de))).toEqual([SEMESTRE, COMPARACAO, MES])
    expect(rotasAtivas[0].params).toMatchObject({ ativo: 'true', pagina: '1', tamanhoPagina: '100' })
    expect(kmDoMes[0].params).toEqual(MES)
  })
})

describe('DashboardPage — precisa de atenção', () => {
  it('lista manutenções, rota esquecida, veículo sem abastecimento e manutenção sem custo', async () => {
    montar()
    expect(await within(atencao()).findByText('7 itens')).toBeInTheDocument()

    const itens = [
      ['Atrasada', 'Troca de óleo', '320 km em atraso', 'Manutenções'],
      ['Atrasada', 'Revisão de freios', 'prazo venceu em 05/11/2026', 'Manutenções'],
      ['Vencendo', 'Alinhamento', 'faltam 400 km', 'Manutenções'],
      ['Manutenção', 'Mais 2 manutenções', 'fora desta lista', 'Manutenções'],
      ['Rota aberta', 'João Pereira', 'aberta há 3 dias', 'Rotas'],
      ['Sem abastecimento', 'ABC1D03', 'nenhum abastecimento lançado', 'Abastecimentos'],
      ['Sem custo', '2 manutenções concluídas', 'custo não informado', 'Manutenções'],
    ]
    for (const [tag, titulo, detalhe, destino] of itens) {
      const linha = within(atencao()).getByText(titulo).closest('div')!
      expect(linha).toHaveTextContent(tag)
      expect(linha).toHaveTextContent(detalhe)
      expect(within(linha).getByRole('link', { name: `${destino} →` })).toBeInTheDocument()
    }
  })

  it('a rota aberta hoje não é pendência', async () => {
    montar()
    await within(atencao()).findByText('João Pereira')
    expect(within(atencao()).queryByText('Carla Souza')).not.toBeInTheDocument()
  })

  it('sem nada pendente, "Tudo em dia"', async () => {
    montar({
      manutencoes: { atrasadas: 0, vencendo: 0, urgentes: [] },
      rotas: [],
      custoDoMes: fabricarResumoCustos({ porVeiculo: [] }),
    })
    expect(await within(atencao()).findByText('Tudo em dia')).toBeInTheDocument()
  })

  it('se uma das consultas falha, avisa que parte não pôde ser verificada', async () => {
    montar({ manutencoesFalham: true })
    expect(await screen.findByText('Parte das pendências não pôde ser verificada. Tente atualizar.')).toBeInTheDocument()
    expect(within(atencao()).queryByText('Tudo em dia')).not.toBeInTheDocument()
  })
})

describe('DashboardPage — rotas e ranking', () => {
  it('rotas em andamento com a estimativa do traçado e a tag da esquecida', async () => {
    montar()
    const rotas = screen.getByRole('region', { name: 'Rotas em andamento' })
    expect(await within(rotas).findByRole('row', { name: /João Pereira/ })).toHaveTextContent('Há 3 dias')
    expect(within(rotas).getByRole('row', { name: /Carla Souza/ })).toHaveTextContent('47 km · 48 min')
    expect(within(rotas).getByRole('link', { name: 'Ver rotas →' })).toHaveAttribute('href', '/rotas')
  })

  it('mais de cinco rotas abertas mostram só cinco, com "Ver todas"', async () => {
    const muitas = Array.from({ length: 7 }, (_, i) => fabricarRota({ id: 100 + i, nomeMotorista: `Pessoa ${i}`, dataInicio: '2026-11-12T00:00:00' }))
    montar({ rotas: muitas })
    const rotas = screen.getByRole('region', { name: 'Rotas em andamento' })
    expect(await within(rotas).findByRole('link', { name: 'Ver todas (7) →' })).toBeInTheDocument()
    expect(within(rotas).getAllByRole('row')).toHaveLength(6)
  })

  it('sem rota aberta', async () => {
    montar({ rotas: [] })
    expect(await screen.findByText('Nenhuma rota em andamento.')).toBeInTheDocument()
  })

  it('o ranking mostra os veículos com custo, do maior para o menor, sem o que não custou', async () => {
    montar()
    const ranking = screen.getByRole('region', { name: 'Veículos que mais custaram no mês' })
    await within(ranking).findByRole('row', { name: /ABC1D01/ })
    const linhas = within(ranking).getAllByRole('row')
    expect(linhas.slice(1).map((l) => within(l).getAllByRole('cell')[0].textContent)).toEqual(['ABC1D01Caminhão 1', 'ABC1D02Caminhão 1'])
    expect(within(ranking).queryByText('ABC1D03')).not.toBeInTheDocument()
  })

  it('sem custo no mês, o ranking diz isso', async () => {
    montar({ custoDoMes: fabricarResumoCustos({ porVeiculo: [] }) })
    expect(await screen.findByText('Nenhum custo lançado no mês.')).toBeInTheDocument()
  })

  it('o painel dos últimos 6 meses aparece com o semestre completo', async () => {
    montar()
    expect(await screen.findByRole('heading', { name: 'Custo dos últimos 6 meses' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^Junho de 2026: sem registros$/ })).toBeInTheDocument()
    expect(screen.getByText(porTextoCompleto(/^Novembro de 2026$/))).toBeInTheDocument()
  })

  it('"Atualizar" refaz as sete consultas', async () => {
    const { user, custos, resumoManutencoes } = montar()
    await esperarKpi('Custo do mês', 'R$ 12 mil')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Atualizar' })).toBeEnabled())
    await user.click(screen.getByRole('button', { name: 'Atualizar' }))
    await waitFor(() => expect(custos).toHaveLength(6))
    expect(resumoManutencoes).toHaveLength(2)
  })
})
