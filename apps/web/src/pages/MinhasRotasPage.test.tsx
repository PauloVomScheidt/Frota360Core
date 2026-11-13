import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ManutencaoResponse, RotaResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { porTextoCompleto } from '../test/consultas'
import { fabricarManutencao, fabricarResumo, fabricarRota, paginado } from '../test/fabricas'
import { PONTOS_FALSOS } from '../test/googleMapsFalso'
import { renderizar } from '../test/renderizar'
import { MinhasRotasPage } from './MinhasRotasPage'

vi.mock('../lib/googleMaps', () => import('../test/googleMapsFalso'))
vi.mock('../components/CampoEndereco', () => import('../test/mapaFalso'))
vi.mock('../components/MapaRota', () => import('../test/mapaFalso'))

const ATIVA = fabricarRota({
  id: 71,
  enderecoPartida: 'Joinville',
  enderecoChegada: 'Curitiba',
  codigoVeiculo: 2,
  veiculoPlaca: 'ABC1D02',
  ativo: true,
  kmInicial: 119_000,
  dataInicio: '2026-11-10T00:00:00',
  dataCalculoRota: '2026-11-10T00:00:00',
  distanciaMetros: 131_400,
  duracaoEstimadaSegundos: 7_920,
})
const HISTORICO = [
  fabricarRota({
    id: 60,
    enderecoPartida: 'Blumenau',
    enderecoChegada: 'Itajaí',
    codigoVeiculo: 1,
    ativo: false,
    kmInicial: 49_588,
    kmFinal: 50_000,
    kmPercorrido: 412,
    dataInicio: '2026-11-03T00:00:00',
    dataFim: '2026-11-04T18:00:00',
  }),
]

const PENDENCIAS: Record<string, ManutencaoResponse[]> = {
  '1': [fabricarManutencao({ tipoManutencaoNome: 'Alinhamento', quilometragemPrevista: 52_000, kmRestantes: 2_000 })],
  '2': [
    fabricarManutencao({ tipoManutencaoNome: 'Revisão de freios', quilometragemPrevista: 120_400, kmRestantes: 400 }),
    fabricarManutencao({ tipoManutencaoNome: 'Troca de óleo', quilometragemPrevista: 118_680, kmRestantes: -1_320, atrasada: true }),
  ],
}

interface Cenario {
  rotaAtiva?: RotaResponse | null
  historico?: RotaResponse[]
  pendencias?: Record<string, ManutencaoResponse[]>
  totalHistorico?: number
  resumoKm?: { quantidade: number; kmTotal: number }
  falhaNasRotas?: boolean
}

function montar({
  rotaAtiva = ATIVA,
  historico = HISTORICO,
  pendencias = PENDENCIAS,
  totalHistorico = historico.length,
  resumoKm = { quantidade: 3, kmTotal: 1234 },
  falhaNasRotas = false,
}: Cenario = {}) {
  const minhas = capturar('get', '/rota/minhas', ({ params }) =>
    falhaNasRotas
      ? responderErro(500, ['Erro interno.'])()
      : responderJson(
          params.ativo === 'true'
            ? paginado(rotaAtiva ? [rotaAtiva] : [], 1, 1)
            : paginado(historico, Number(params.pagina), Number(params.tamanhoPagina), totalHistorico),
        )(),
  )
  const kmDoMes = capturar('get', '/rota/resumo', responderJson(resumoKm))
  const abastecimentosDoMes = capturar(
    'get',
    '/abastecimento/resumo',
    responderJson(fabricarResumo({ quantidade: 4, valorTotal: 812.3 })),
  )
  const manutencoes = capturar('get', '/manutencao', ({ params }) => responderJson(paginado(pendencias[params.veiculoId] ?? []))())
  const calculo = capturar(
    'post',
    '/rota/calcular',
    responderJson({ distanciaMetros: 131_400, duracaoEstimadaSegundos: 7_920, polylineCodificada: 'poly' }),
  )

  const tela = renderizar(<MinhasRotasPage />, { rota: '/minhas-rotas', papel: 'Motorista' })
  return { ...tela, minhas, kmDoMes, abastecimentosDoMes, manutencoes, calculo }
}

const meuMes = () => screen.getByRole('region', { name: 'Meu mês' })

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date(2026, 10, 12, 10, 0))
})

afterEach(() => vi.useRealTimers())

describe('MinhasRotasPage — meu mês', () => {
  it('o km do mês vem de /rota/resumo, do dia 1 até hoje', async () => {
    const { kmDoMes } = montar()
    expect(await within(meuMes()).findByText('1.234')).toBeInTheDocument()
    expect(within(meuMes()).getByText('3 rotas encerradas')).toBeInTheDocument()
    expect(kmDoMes[0].params).toEqual({ de: '2026-11-01', ate: '2026-11-12' })
  })

  it('uma rota encerrada no singular', async () => {
    montar({ resumoKm: { quantidade: 1, kmTotal: 412 } })
    expect(await within(meuMes()).findByText('1 rota encerrada')).toBeInTheDocument()
  })

  it('os abastecimentos do mês vêm de /abastecimento/resumo, com o mesmo intervalo', async () => {
    const { abastecimentosDoMes } = montar()
    expect(await within(meuMes()).findByText(porTextoCompleto('no mês · R$ 812,30'))).toBeInTheDocument()
    expect(within(meuMes()).getByText('4')).toBeInTheDocument()
    expect(abastecimentosDoMes[0].params).toEqual({ de: '2026-11-01', ate: '2026-11-12' })
  })

  it('o cartão de manutenção mostra a pendência mais urgente do veículo da rota — atrasada primeiro', async () => {
    const { manutencoes } = montar()
    expect(await within(meuMes()).findByText('Manutenção do ABC1D02')).toBeInTheDocument()
    expect(await within(meuMes()).findByText('Atrasada')).toBeInTheDocument()
    expect(within(meuMes()).getByText(porTextoCompleto('Troca de óleo · 1.320 km em atraso'))).toBeInTheDocument()
    expect(manutencoes[0].params).toMatchObject({ status: 'Pendente', veiculoId: '2' })
  })

  it('veículo da rota sem pendência está em dia', async () => {
    montar({ pendencias: {} })
    expect(await within(meuMes()).findByText('Em dia')).toBeInTheDocument()
    expect(within(meuMes()).getByText('Nenhuma manutenção pendente.')).toBeInTheDocument()
  })

  it('sem rota aberta, o cartão pede para abrir uma e nem consulta manutenção', async () => {
    const { manutencoes } = montar({ rotaAtiva: null })
    expect(await within(meuMes()).findByText('Abra uma rota para ver a situação do veículo.')).toBeInTheDocument()
    expect(manutencoes).toHaveLength(0)
  })
})

describe('MinhasRotasPage — rota em andamento', () => {
  it('aparece em destaque, com veículo, abertura e a estimativa do traçado', async () => {
    montar()
    expect(await screen.findByText('Joinville → Curitiba')).toBeInTheDocument()
    expect(
      screen.getByText(
        porTextoCompleto(
          'ABC1D02 — Caminhão 2 · aberta em 10/11/2026 · saiu com 119.000 km · estimativa de 131 km em 2 h 12 min',
        ),
      ),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Lançar abastecimento' })).toHaveAttribute('href', '/abastecimentos')
  })

  it('a rota ativa é pedida à parte, sem depender da página do histórico', async () => {
    const { minhas } = montar()
    await screen.findByText('Joinville → Curitiba')
    expect(minhas.map((c) => c.params)).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ ativo: 'true', pagina: '1', tamanhoPagina: '1' }),
        expect.objectContaining({ ativo: 'false', pagina: '1', tamanhoPagina: '15' }),
      ]),
    )
  })

  it('com rota aberta não dá para abrir outra, e o botão diz por quê', async () => {
    montar()
    await screen.findByText('Joinville → Curitiba')
    const botao = screen.getByRole('button', { name: 'Abrir rota' })
    expect(botao).toBeDisabled()
    expect(botao).toHaveAttribute('title', 'Encerre a rota em andamento antes de abrir outra.')
  })

  it('sem rota aberta, convida a abrir', async () => {
    montar({ rotaAtiva: null })
    expect(await screen.findByText(/Nenhuma rota aberta no momento/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Abrir rota' })).toBeEnabled()
  })

  it('falha ao carregar as rotas aparece como erro', async () => {
    montar({ falhaNasRotas: true })
    expect(await screen.findAllByText('Erro interno.')).not.toHaveLength(0)
  })
})

describe('MinhasRotasPage — encerrar', () => {
  async function abrirEncerramento(user: UserEvent) {
    await user.click(await screen.findByRole('button', { name: 'Encerrar rota' }))
    return screen.getByRole('dialog', { name: 'Encerrar rota' })
  }

  it('abre com o odômetro do veículo e a data de hoje, e envia o km final', async () => {
    const encerrar = capturar('post', '/rota/:id/encerrar', responderJson({ ...ATIVA, ativo: false }))
    const { user, minhas } = montar()
    const dialogo = await abrirEncerramento(user)

    expect(within(dialogo).getByLabelText('Quilometragem final')).toHaveValue(120_000)
    expect(within(dialogo).getByLabelText('Data de fim (opcional)')).toHaveValue('2026-11-12')

    await user.clear(within(dialogo).getByLabelText('Quilometragem final'))
    await user.type(within(dialogo).getByLabelText('Quilometragem final'), '120412')
    await user.click(within(dialogo).getByRole('button', { name: 'Encerrar' }))

    await waitFor(() => expect(encerrar).toHaveLength(1))
    expect(encerrar[0].url.pathname).toMatch(/\/rota\/71\/encerrar$/)
    expect(encerrar[0].corpo).toEqual({ kmFinal: 120_412, dataFim: '2026-11-12' })
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    await waitFor(() => expect(minhas.length).toBeGreaterThan(2))
  })

  it('um 422 no encerramento aparece literal', async () => {
    const mensagem = 'A data de fim não pode ser anterior à data de início.'
    capturar('post', '/rota/:id/encerrar', responderErro(422, [mensagem]))
    const { user } = montar()
    const dialogo = await abrirEncerramento(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Encerrar' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
  })

  it('cancelar fecha o diálogo', async () => {
    const { user } = montar()
    const dialogo = await abrirEncerramento(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Cancelar' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})

describe('MinhasRotasPage — abrir rota', () => {
  async function abrirFormulario(user: UserEvent) {
    await user.click(await screen.findByRole('button', { name: 'Abrir rota' }))
    return screen.getByRole('dialog', { name: 'Abrir rota' })
  }

  it('não tem seletor de motorista e já vem com a data de hoje', async () => {
    const { user } = montar({ rotaAtiva: null })
    const dialogo = await abrirFormulario(user)
    expect(within(dialogo).queryByLabelText('Motorista')).not.toBeInTheDocument()
    expect(within(dialogo).getByLabelText('Início')).toHaveValue('2026-11-12')
  })

  it('o POST sai sem codigoMotorista, com o trajeto calculado e o km sugerido', async () => {
    const abrir = capturar('post', '/rota', responderJson(ATIVA))
    const { user, calculo } = montar({ rotaAtiva: null })
    const dialogo = await abrirFormulario(user)

    await user.click(within(dialogo).getByRole('button', { name: 'Escolher sugestão para Partida' }))
    await user.click(within(dialogo).getByRole('button', { name: 'Escolher sugestão para Chegada' }))
    await waitFor(() => expect(calculo).toHaveLength(1))
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    expect(within(dialogo).getByLabelText('Quilometragem inicial')).toHaveValue(50_000)
    await user.click(within(dialogo).getByRole('button', { name: 'Abrir rota' }))

    await waitFor(() => expect(abrir).toHaveLength(1))
    expect(abrir[0].corpo).toEqual({
      enderecoPartida: PONTOS_FALSOS.enderecoPartida.endereco,
      enderecoChegada: PONTOS_FALSOS.enderecoChegada.endereco,
      latitudePartida: PONTOS_FALSOS.enderecoPartida.latitude,
      longitudePartida: PONTOS_FALSOS.enderecoPartida.longitude,
      latitudeChegada: PONTOS_FALSOS.enderecoChegada.latitude,
      longitudeChegada: PONTOS_FALSOS.enderecoChegada.longitude,
      distanciaMetros: 131_400,
      duracaoEstimadaSegundos: 7_920,
      polylineCodificada: 'poly',
      codigoVeiculo: 1,
      dataInicio: '2026-11-12',
      kmInicial: 50_000,
    })
    expect(abrir[0].corpo).not.toHaveProperty('codigoMotorista')
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })

  it('a sugestão de km não sobrescreve um número digitado', async () => {
    const { user } = montar({ rotaAtiva: null })
    const dialogo = await abrirFormulario(user)
    const km = within(dialogo).getByLabelText('Quilometragem inicial')
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    await user.clear(km)
    await user.type(km, '50100')
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2 (120.000 km)')
    expect(km).toHaveValue(50_100)
  })

  it.each([
    ['ABC1D02 — Caminhão 2 (120.000 km)', 'Este veículo tem manutenção atrasada.', ['Troca de óleo · prevista em 118.680 km · atrasada', 'Revisão de freios · prevista em 120.400 km · faltam 400 km']],
    ['ABC1D01 — Caminhão 1 (50.000 km)', 'Este veículo tem manutenção prevista.', ['Alinhamento · prevista em 52.000 km · faltam 2.000 km']],
  ])('escolher %s mostra o aviso "%s", atrasadas primeiro, sem bloquear', async (veiculo, titulo, itens) => {
    const { user, manutencoes } = montar({ rotaAtiva: null })
    const dialogo = await abrirFormulario(user)
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), veiculo)

    expect(await within(dialogo).findByText(titulo)).toBeInTheDocument()
    expect(within(dialogo).getAllByRole('listitem').map((li) => li.textContent)).toEqual(itens)
    expect(manutencoes.at(-1)?.params).toMatchObject({ status: 'Pendente' })
    expect(within(dialogo).getByRole('button', { name: 'Abrir rota' })).toBeEnabled()
  })

  it('só vencendo no veículo escolhido avisa "vencendo"', async () => {
    const { user } = montar({ rotaAtiva: null, pendencias: { '1': [PENDENCIAS['2'][0]] } })
    const dialogo = await abrirFormulario(user)
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    expect(await within(dialogo).findByText('Este veículo tem manutenção vencendo.')).toBeInTheDocument()
  })

  it('um 422 ao abrir aparece literal', async () => {
    const mensagem = 'Você já tem uma rota em andamento.'
    capturar('post', '/rota', responderErro(422, [mensagem]))
    const { user } = montar({ rotaAtiva: null })
    const dialogo = await abrirFormulario(user)
    await user.type(within(dialogo).getByLabelText('Partida'), 'Joinville')
    await user.type(within(dialogo).getByLabelText('Chegada'), 'Curitiba')
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    await user.click(within(dialogo).getByRole('button', { name: 'Abrir rota' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
  })

  it('sem veículo cadastrado, o botão fica desabilitado com o motivo', async () => {
    capturar('get', '/veiculo', responderJson([]))
    montar({ rotaAtiva: null })
    const botao = await screen.findByRole('button', { name: 'Abrir rota' })
    await waitFor(() => expect(botao).toBeDisabled())
    expect(botao).toHaveAttribute('title', 'Nenhum veículo cadastrado — fale com o supervisor.')
  })
})

describe('MinhasRotasPage — histórico', () => {
  it('lista as rotas encerradas com o km percorrido e o intervalo de odômetro', async () => {
    montar()
    const celulas = within(await screen.findByRole('row', { name: /Blumenau → Itajaí/ })).getAllByRole('cell')
    expect(celulas[1]).toHaveTextContent('ABC1D01')
    expect(celulas[4]).toHaveTextContent('412 km49.588 km → 50.000 km')
    expect(celulas[5]).toHaveTextContent('Encerrada')
  })

  it('virar de página pede o histórico seguinte ao servidor', async () => {
    const { user, minhas } = montar({ totalHistorico: 30 })
    await screen.findByText('1–15 de 30')
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(minhas.at(-1)?.params).toMatchObject({ ativo: 'false', pagina: '2' }))
  })

  it('sem rota encerrada, diz isso', async () => {
    montar({ historico: [] })
    expect(await screen.findByText('Você ainda não encerrou nenhuma rota.')).toBeInTheDocument()
  })
})
