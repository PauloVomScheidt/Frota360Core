import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Role } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarRota, paginado } from '../test/fabricas'
import { porTextoCompleto } from '../test/consultas'
import { PONTOS_FALSOS } from '../test/googleMapsFalso'
import { renderizar } from '../test/renderizar'
import { RotasPage } from './RotasPage'

vi.mock('../lib/googleMaps', () => import('../test/googleMapsFalso'))
vi.mock('../components/CampoEndereco', () => import('../test/mapaFalso'))
vi.mock('../components/MapaRota', () => import('../test/mapaFalso'))

const ATIVA = fabricarRota({
  id: 71,
  enderecoPartida: 'Joinville',
  enderecoChegada: 'Curitiba',
  nomeMotorista: 'João Pereira',
  codigoMotorista: 10,
  codigoVeiculo: 2,
  veiculoPlaca: 'ABC1D02',
  ativo: true,
  kmInicial: 119_000,
  dataInicio: '2026-11-10T00:00:00',
})
const ENCERRADA = fabricarRota({
  id: 72,
  enderecoPartida: 'Blumenau',
  enderecoChegada: 'Itajaí',
  nomeMotorista: 'Carla Souza',
  codigoMotorista: 11,
  codigoVeiculo: 1,
  veiculoPlaca: 'ABC1D01',
  ativo: false,
  kmInicial: 50_000,
  kmFinal: 50_412,
  kmPercorrido: 412,
  dataFim: '2026-11-05T18:00:00',
  dataCalculoRota: '2026-11-01T08:00:00',
  latitudePartida: -26.9,
  longitudePartida: -49.06,
  latitudeChegada: -26.9,
  longitudeChegada: -48.66,
  distanciaMetros: 47_000,
  duracaoEstimadaSegundos: 2_880,
  polylineCodificada: 'poly-salva',
})

const CALCULO = { distanciaMetros: 131_400, duracaoEstimadaSegundos: 7_920, polylineCodificada: 'poly-nova' }

function montar(papel: Role = 'Admin', total = 2) {
  const lista = capturar('get', '/rota', ({ params }) =>
    responderJson(paginado([ATIVA, ENCERRADA], Number(params.pagina), Number(params.tamanhoPagina), total))(),
  )
  const calculo = capturar('post', '/rota/calcular', responderJson(CALCULO))
  const tela = renderizar(<RotasPage />, { rota: '/rotas', papel })
  return { ...tela, lista, calculo }
}

const linha = (texto: string) => screen.findByRole('row', { name: new RegExp(texto) })

async function abrirNova(user: UserEvent) {
  await user.click(await screen.findByRole('button', { name: 'Nova rota' }))
  return screen.getByRole('dialog', { name: 'Nova rota' })
}

async function escolherTrajeto(user: UserEvent, dialogo: HTMLElement) {
  await user.click(within(dialogo).getByRole('button', { name: 'Escolher sugestão para Partida' }))
  await user.click(within(dialogo).getByRole('button', { name: 'Escolher sugestão para Chegada' }))
}

async function preencherResto(user: UserEvent, dialogo: HTMLElement) {
  await user.type(within(dialogo).getByLabelText('Início'), '2026-11-12')
  await user.selectOptions(within(dialogo).getByLabelText('Motorista'), 'João Pereira')
  await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2 (120.000 km)')
}

afterEach(() => vi.useRealTimers())

describe('RotasPage — listagem', () => {
  it('rota ativa mostra o km de abertura; encerrada, o percorrido e o intervalo', async () => {
    montar()
    const ativa = within(await linha('Joinville → Curitiba')).getAllByRole('cell')
    expect(ativa[5]).toHaveTextContent('—desde 119.000 km')
    expect(ativa[6]).toHaveTextContent('Ativa')

    const encerrada = within(await linha('Blumenau → Itajaí')).getAllByRole('cell')
    expect(encerrada[5]).toHaveTextContent('412 km50.000 km → 50.412 km')
    expect(encerrada[6]).toHaveTextContent('Encerrada')
  })

  it('rota sem o nome desnormalizado do motorista mostra o código', async () => {
    capturar('get', '/rota', responderJson(paginado([fabricarRota({ enderecoPartida: 'Lages', nomeMotorista: null, codigoMotorista: 99 })])))
    renderizar(<RotasPage />, { papel: 'Admin' })
    expect(within(await linha('Lages')).getByText('#99')).toBeInTheDocument()
  })

  it('"Encerrar" só aparece em rota ativa', async () => {
    montar()
    expect(within(await linha('Joinville → Curitiba')).getByRole('button', { name: 'Encerrar' })).toBeInTheDocument()
    expect(within(await linha('Blumenau → Itajaí')).queryByRole('button', { name: 'Encerrar' })).not.toBeInTheDocument()
  })

  it.each<[Role, boolean]>([
    ['Admin', true],
    ['Supervisor', true],
    ['Operador', false],
  ])('%s: excluir visível = %s (editar e encerrar continuam para toda a gestão)', async (papel, excluir) => {
    montar(papel)
    const ativa = await linha('Joinville → Curitiba')
    expect(within(ativa).getByRole('button', { name: 'Encerrar' })).toBeInTheDocument()
    expect(within(ativa).getByRole('button', { name: /^Editar/ })).toBeInTheDocument()
    expect(within(ativa).queryAllByRole('button', { name: /^Excluir/ })).toHaveLength(excluir ? 1 : 0)
  })

  it('virar de página pede a página seguinte ao servidor', async () => {
    const { user, lista } = montar('Admin', 40)
    await screen.findByText('1–15 de 40')
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ pagina: '2', tamanhoPagina: '15' }))
  })

  it('sem motorista cadastrado não dá para abrir rota', async () => {
    capturar('get', '/motorista', responderJson([]))
    montar()
    const botao = await screen.findByRole('button', { name: 'Nova rota' })
    await waitFor(() => expect(botao).toBeDisabled())
    expect(botao).toHaveAttribute('title', 'Cadastre ao menos um motorista e um veículo antes.')
  })
})

describe('RotasPage — abrir rota', () => {
  it('escolher partida e chegada calcula o trajeto no POST /rota/calcular', async () => {
    const { user, calculo } = montar()
    const dialogo = await abrirNova(user)
    await escolherTrajeto(user, dialogo)

    await waitFor(() => expect(calculo).toHaveLength(1))
    expect(calculo[0].corpo).toEqual({
      latitudeOrigem: PONTOS_FALSOS.enderecoPartida.latitude,
      longitudeOrigem: PONTOS_FALSOS.enderecoPartida.longitude,
      latitudeDestino: PONTOS_FALSOS.enderecoChegada.latitude,
      longitudeDestino: PONTOS_FALSOS.enderecoChegada.longitude,
    })
    expect(await within(dialogo).findByText(porTextoCompleto('131 km · 2 h 12 min estimados pelo Google'))).toBeInTheDocument()
    expect(within(dialogo).getByRole('img', { name: 'Mapa com o traçado poly-nova' })).toBeInTheDocument()
  })

  it('só um ponto não calcula nada — cada cálculo é cobrado', async () => {
    const { user, calculo } = montar()
    const dialogo = await abrirNova(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Escolher sugestão para Partida' }))

    expect(within(dialogo).getByRole('img', { name: 'Mapa sem traçado' })).toBeInTheDocument()
    expect(calculo).toHaveLength(0)
  })

  it('sugere a quilometragem inicial com o odômetro do veículo', async () => {
    const { user } = montar()
    const dialogo = await abrirNova(user)
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2 (120.000 km)')
    expect(within(dialogo).getByLabelText('Quilometragem inicial')).toHaveValue(120_000)

    await user.clear(within(dialogo).getByLabelText('Quilometragem inicial'))
    await user.type(within(dialogo).getByLabelText('Quilometragem inicial'), '121000')
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1 (50.000 km)')
    expect(within(dialogo).getByLabelText('Quilometragem inicial')).toHaveValue(121_000)
  })

  it('o POST leva endereços, coordenadas, o traçado calculado e o km de abertura', async () => {
    const criar = capturar('post', '/rota', responderJson(ATIVA))
    const { user, lista } = montar()
    const dialogo = await abrirNova(user)
    await escolherTrajeto(user, dialogo)
    await within(dialogo).findByText(/estimados pelo Google/)
    await preencherResto(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toEqual({
      enderecoPartida: PONTOS_FALSOS.enderecoPartida.endereco,
      enderecoChegada: PONTOS_FALSOS.enderecoChegada.endereco,
      latitudePartida: PONTOS_FALSOS.enderecoPartida.latitude,
      longitudePartida: PONTOS_FALSOS.enderecoPartida.longitude,
      latitudeChegada: PONTOS_FALSOS.enderecoChegada.latitude,
      longitudeChegada: PONTOS_FALSOS.enderecoChegada.longitude,
      ...CALCULO,
      codigoMotorista: 10,
      codigoVeiculo: 2,
      dataInicio: '2026-11-12',
      kmInicial: 120_000,
    })
    expect(criar[0].corpo).not.toHaveProperty('dataCalculoRota')
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('digitar o endereço sem escolher da lista salva sem coordenada e sem traçado', async () => {
    const criar = capturar('post', '/rota', responderJson(ATIVA))
    const { user, calculo } = montar()
    const dialogo = await abrirNova(user)
    await user.type(within(dialogo).getByLabelText('Partida'), 'Joinville')
    await user.type(within(dialogo).getByLabelText('Chegada'), 'Curitiba')
    await preencherResto(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toMatchObject({
      enderecoPartida: 'Joinville',
      enderecoChegada: 'Curitiba',
      latitudePartida: null,
      longitudeChegada: null,
      distanciaMetros: null,
      polylineCodificada: null,
    })
    expect(calculo).toHaveLength(0)
  })

  it('trocar um ponto depois do cálculo zera o traçado e a coordenada daquele ponto', async () => {
    const criar = capturar('post', '/rota', responderJson(ATIVA))
    const { user } = montar()
    const dialogo = await abrirNova(user)
    await escolherTrajeto(user, dialogo)
    await within(dialogo).findByText(/estimados pelo Google/)

    await user.type(within(dialogo).getByLabelText('Chegada'), ' (portão 2)')
    expect(within(dialogo).queryByText(/estimados pelo Google/)).not.toBeInTheDocument()

    await preencherResto(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))
    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toMatchObject({
      latitudePartida: PONTOS_FALSOS.enderecoPartida.latitude,
      latitudeChegada: null,
      distanciaMetros: null,
      duracaoEstimadaSegundos: null,
      polylineCodificada: null,
    })
  })

  it('cálculo recusado (422) vira aviso literal e não impede cadastrar', async () => {
    const aviso = 'Não foi possível calcular o trajeto agora. Tente novamente em instantes.'
    const criar = capturar('post', '/rota', responderJson(ATIVA))
    const { user } = montar()
    capturar('post', '/rota/calcular', responderErro(422, [aviso]))
    const dialogo = await abrirNova(user)
    await escolherTrajeto(user, dialogo)

    expect(await within(dialogo).findByRole('status')).toHaveTextContent(
      `${aviso} A rota pode ser cadastrada assim — o trajeto é opcional.`,
    )
    await preencherResto(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))
    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toMatchObject({ distanciaMetros: null })
  })

  it('um 422 ao cadastrar aparece literal no formulário', async () => {
    const mensagem = 'A quilometragem inicial (100.000 km) não pode ser menor que o odômetro atual do veículo (120.000 km).'
    capturar('post', '/rota', responderErro(422, [mensagem]))
    const { user } = montar()
    const dialogo = await abrirNova(user)
    await user.type(within(dialogo).getByLabelText('Partida'), 'Joinville')
    await user.type(within(dialogo).getByLabelText('Chegada'), 'Curitiba')
    await preencherResto(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
  })
})

describe('RotasPage — editar', () => {
  it('rota com traçado salvo abre com ele e NÃO recalcula; o PUT não leva kmInicial', async () => {
    const atualizar = capturar('put', '/rota/:id', responderJson(ENCERRADA))
    const { user, calculo } = montar()
    await user.click(within(await linha('Blumenau → Itajaí')).getByRole('button', { name: /^Editar/ }))
    const dialogo = screen.getByRole('dialog', { name: 'Editar rota' })

    expect(within(dialogo).getByText(porTextoCompleto('47 km · 48 min estimados pelo Google'))).toBeInTheDocument()
    expect(within(dialogo).queryByLabelText('Quilometragem inicial')).not.toBeInTheDocument()

    await user.click(within(dialogo).getByRole('button', { name: 'Salvar alterações' }))
    await waitFor(() => expect(atualizar).toHaveLength(1))
    expect(calculo).toHaveLength(0)
    expect(atualizar[0].url.pathname).toMatch(/\/rota\/72$/)
    expect(atualizar[0].corpo).toMatchObject({ distanciaMetros: 47_000, polylineCodificada: 'poly-salva', dataInicio: '2026-11-10' })
    expect(atualizar[0].corpo).not.toHaveProperty('kmInicial')
  })

  it('rota sem traçado (dataCalculoRota nula) não trata as coordenadas zeradas como ponto', async () => {
    const atualizar = capturar('put', '/rota/:id', responderJson(ATIVA))
    const { user } = montar()
    await user.click(within(await linha('Joinville → Curitiba')).getByRole('button', { name: /^Editar/ }))
    const dialogo = screen.getByRole('dialog', { name: 'Editar rota' })

    expect(within(dialogo).queryByRole('img')).not.toBeInTheDocument()
    await user.click(within(dialogo).getByRole('button', { name: 'Salvar alterações' }))
    await waitFor(() => expect(atualizar).toHaveLength(1))
    expect(atualizar[0].corpo).toMatchObject({ latitudePartida: null, distanciaMetros: null })
  })
})

describe('RotasPage — motorista que saiu da lista', () => {
  it('editar a rota de um motorista rebaixado salva sem trocar o motorista', async () => {
    const antiga = fabricarRota({ id: 80, enderecoPartida: 'Lages', nomeMotorista: 'Ex-motorista', codigoMotorista: 99 })
    capturar('get', '/rota', responderJson(paginado([antiga])))
    const atualizar = capturar('put', '/rota/:id', responderJson(antiga))
    const { user } = renderizar(<RotasPage />, { papel: 'Admin' })

    await user.click(within(await linha('Lages')).getByRole('button', { name: /^Editar/ }))
    const dialogo = screen.getByRole('dialog', { name: 'Editar rota' })
    expect(within(dialogo).getByLabelText('Motorista')).toHaveDisplayValue('Ex-motorista (não é mais motorista)')
    await user.click(within(dialogo).getByRole('button', { name: 'Salvar alterações' }))

    await waitFor(() => expect(atualizar).toHaveLength(1))
    expect(atualizar[0].corpo).toMatchObject({ codigoMotorista: 99 })
  })
})

describe('RotasPage — encerrar', () => {
  async function abrirEncerramento(user: UserEvent) {
    await user.click(within(await linha('Joinville → Curitiba')).getByRole('button', { name: 'Encerrar' }))
    return screen.getByRole('dialog', { name: 'Encerrar rota' })
  }

  it('abre com o odômetro atual do veículo e a data de hoje', async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 10, 12, 10, 0))
    const { user } = montar()
    const dialogo = await abrirEncerramento(user)

    expect(within(dialogo).getByLabelText('Quilometragem final')).toHaveValue(120_000)
    expect(within(dialogo).getByLabelText('Data de fim (opcional)')).toHaveValue('2026-11-12')
  })

  it('envia o km final e recarrega a lista', async () => {
    const encerrar = capturar('post', '/rota/:id/encerrar', responderJson(ENCERRADA))
    const { user, lista } = montar()
    const dialogo = await abrirEncerramento(user)

    await user.clear(within(dialogo).getByLabelText('Quilometragem final'))
    await user.type(within(dialogo).getByLabelText('Quilometragem final'), '120412')
    await user.clear(within(dialogo).getByLabelText('Data de fim (opcional)'))
    await user.click(within(dialogo).getByRole('button', { name: 'Encerrar' }))

    await waitFor(() => expect(encerrar).toHaveLength(1))
    expect(encerrar[0].url.pathname).toMatch(/\/rota\/71\/encerrar$/)
    expect(encerrar[0].corpo).toEqual({ kmFinal: 120_412, dataFim: null })
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('um 422 no encerramento aparece literal', async () => {
    const mensagem = 'A quilometragem final não pode ser menor que a inicial.'
    capturar('post', '/rota/:id/encerrar', responderErro(422, [mensagem]))
    const { user } = montar()
    const dialogo = await abrirEncerramento(user)
    await user.click(within(dialogo).getByRole('button', { name: 'Encerrar' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
  })
})

describe('RotasPage — excluir', () => {
  it('exclui depois de confirmar', async () => {
    const excluir = capturar('delete', '/rota/:id')
    const { user } = montar()
    await user.click(within(await linha('Blumenau → Itajaí')).getByRole('button', { name: /^Excluir/ }))
    await user.click(within(screen.getByRole('alertdialog', { name: 'Excluir rota' })).getByRole('button', { name: 'Excluir' }))

    await waitFor(() => expect(excluir).toHaveLength(1))
    expect(excluir[0].url.pathname).toMatch(/\/rota\/72$/)
  })

  it('um 422 na exclusão aparece literal', async () => {
    const mensagem = 'Rota com abastecimentos vinculados não pode ser excluída.'
    capturar('delete', '/rota/:id', responderErro(422, [mensagem]))
    const { user } = montar()
    await user.click(within(await linha('Joinville → Curitiba')).getByRole('button', { name: /^Excluir/ }))
    const confirmacao = screen.getByRole('alertdialog')
    await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))
    expect(await within(confirmacao).findByText(mensagem)).toBeInTheDocument()
  })
})
