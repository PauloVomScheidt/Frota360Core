import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { Role, RotaResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { normalizar, porTextoCompleto } from '../test/consultas'
import { fabricarAbastecimento, fabricarResumo, fabricarRota, paginado } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { AbastecimentosPage } from './AbastecimentosPage'

const LANCAMENTOS = [
  fabricarAbastecimento({
    id: 501,
    veiculoId: 1,
    veiculoPlaca: 'ABC1D01',
    motoristaNome: 'João Pereira',
    usuarioNome: 'Supervisora Ana',
    litros: 100,
    valorLitro: 6.5,
    notaFiscal: '000123',
    dataAbastecimento: '2026-11-10T09:00:00',
  }),
  fabricarAbastecimento({
    id: 502,
    veiculoId: 2,
    veiculoPlaca: 'ABC1D02',
    motoristaNome: 'Carla Souza',
    usuarioNome: 'Carla Souza',
    litros: 50,
    valorLitro: 6,
    notaFiscal: '000124',
    dataAbastecimento: '2026-11-11T09:00:00',
  }),
]

interface Cenario {
  papel?: Role
  rotaAtiva?: RotaResponse | null
  totalNoServidor?: number
}

function montar({ papel = 'Admin', rotaAtiva = null, totalNoServidor = LANCAMENTOS.length }: Cenario = {}) {
  const lista = capturar('get', '/abastecimento', ({ params }) =>
    responderJson(paginado(LANCAMENTOS, Number(params.pagina), Number(params.tamanhoPagina), totalNoServidor))(),
  )
  const resumo = capturar('get', '/abastecimento/resumo', responderJson(fabricarResumo({ quantidade: 37, valorTotal: 9876.5 })))
  const anterior = capturar('get', '/abastecimento/anterior', responderJson(null))
  const minhasRotas = capturar('get', '/rota/minhas', responderJson(paginado(rotaAtiva ? [rotaAtiva] : [])))

  const tela = renderizar(<AbastecimentosPage />, { rota: '/abastecimentos', papel })
  return { ...tela, lista, resumo, anterior, minhasRotas }
}

async function abrirNovo(user: UserEvent) {
  await user.click(await screen.findByRole('button', { name: 'Novo abastecimento' }))
  return screen.getByRole('dialog', { name: 'Novo abastecimento' })
}

async function preencherAbastecimento(user: UserEvent, dialogo: HTMLElement, litros = '40', valorLitro = '6.1') {
  const campo = (rotulo: string) => within(dialogo).getByLabelText(rotulo)
  await user.selectOptions(campo('Combustível'), 'Diesel S10')
  await user.type(campo('Litros'), litros)
  await user.type(campo('Valor do litro (R$)'), valorLitro)
  await user.type(campo('Odômetro (km)'), '50500')
  await user.selectOptions(campo('Posto'), 'Posto Central — Joinville')
  await user.type(campo('Nota fiscal'), 'NF-987')
}

describe('AbastecimentosPage — listagem', () => {
  it('mostra os lançamentos da página que o servidor devolveu', async () => {
    montar()
    expect(await screen.findByText('NF 000123')).toBeInTheDocument()
    expect(screen.getByText('NF 000124')).toBeInTheDocument()
  })

  it('pede a primeira página com o tamanho preferido', async () => {
    const { lista } = montar()
    await screen.findByText('NF 000123')
    expect(lista[0].params).toMatchObject({ pagina: '1', tamanhoPagina: '15' })
  })

  it('o rodapé vem de /abastecimento/resumo, não da soma das linhas da página', async () => {
    montar()
    expect(await screen.findByText(porTextoCompleto('37 lançamentos'))).toBeInTheDocument()
    expect(screen.getByText(porTextoCompleto('Total: R$ 9.876,50'))).toBeInTheDocument()
  })

  it('a gestão vê a coluna "Quem lançou"', async () => {
    montar({ papel: 'Supervisor' })
    expect(await screen.findByRole('columnheader', { name: 'Quem lançou' })).toBeInTheDocument()
    expect(await screen.findByText('Supervisora Ana')).toBeInTheDocument()
  })

  it('para o motorista a coluna "Quem lançou" e o filtro por motorista somem', async () => {
    montar({ papel: 'Motorista' })
    await screen.findByText('NF 000123')
    expect(screen.queryByRole('columnheader', { name: 'Quem lançou' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Filtrar por motorista')).not.toBeInTheDocument()
  })

  it('virar de página pede a página seguinte, e o rodapé de totais não muda', async () => {
    const { user, lista, resumo } = montar({ totalNoServidor: 37 })
    await screen.findByText('1–15 de 37')

    await user.click(screen.getByRole('button', { name: 'Próxima' }))

    await waitFor(() => expect(lista.at(-1)?.params.pagina).toBe('2'))
    expect(resumo).toHaveLength(1)
    expect(screen.getByText(porTextoCompleto('37 lançamentos'))).toBeInTheDocument()
  })

  it('filtrar por veículo volta para a página 1 e recorta lista e resumo', async () => {
    const { user, lista, resumo } = montar({ totalNoServidor: 37 })
    await screen.findByText('1–15 de 37')
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(lista.at(-1)?.params.pagina).toBe('2'))

    await user.selectOptions(screen.getByLabelText('Filtrar por veículo'), 'ABC1D02 — Caminhão 2')

    await waitFor(() => expect(lista.at(-1)?.params).toMatchObject({ veiculoId: '2', pagina: '1' }))
    expect(resumo.at(-1)?.params).toMatchObject({ veiculoId: '2' })
    expect(resumo.at(-1)?.params.pagina).toBeUndefined()
  })

  it('sem posto credenciado não dá para lançar, e o botão diz por quê', async () => {
    capturar('get', '/posto', responderJson([]))
    montar()
    const botao = await screen.findByRole('button', { name: 'Novo abastecimento' })
    await waitFor(() => expect(botao).toBeDisabled())
    expect(botao).toHaveAttribute('title', 'É preciso ter ao menos um posto credenciado.')
  })
})

describe('AbastecimentosPage — valor total', () => {
  it('é só leitura e espelha litros × R$/litro', async () => {
    const { user } = montar()
    const dialogo = await abrirNovo(user)
    const total = within(dialogo).getByLabelText('Valor total (R$)')

    expect(total).toHaveAttribute('readonly')
    await user.type(within(dialogo).getByLabelText('Litros'), '48.5')
    await user.type(within(dialogo).getByLabelText('Valor do litro (R$)'), '6.19')
    expect(normalizar((total as HTMLInputElement).value)).toBe('R$ 300,22')

    await user.type(total, '999')
    expect(normalizar((total as HTMLInputElement).value)).toBe('R$ 300,22')
  })

  it('espelha o valor que a API grava também no meio centavo (20,5 L × R$ 5,39)', async () => {
    const { user } = montar()
    const dialogo = await abrirNovo(user)
    await user.type(within(dialogo).getByLabelText('Litros'), '20.5')
    await user.type(within(dialogo).getByLabelText('Valor do litro (R$)'), '5.39')
    const total = within(dialogo).getByLabelText('Valor total (R$)') as HTMLInputElement
    expect(normalizar(total.value)).toBe('R$ 110,50')
  })
})

describe('AbastecimentosPage — lançar pela gestão', () => {
  it('o POST leva o apontamento fiscal e o motorista escolhido, mas não o valor', async () => {
    const criar = capturar('post', '/abastecimento', responderJson(fabricarAbastecimento()))
    const { user, lista } = montar()
    const dialogo = await abrirNovo(user)

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1')
    await user.selectOptions(within(dialogo).getByLabelText('Motorista'), 'João Pereira')
    await preencherAbastecimento(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Lançar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toMatchObject({
      veiculoId: 1,
      motoristaId: 10,
      tipoCombustivelId: 1,
      postoId: 1,
      litros: 40,
      valorLitro: 6.1,
      odometro: 50500,
      notaFiscal: 'NF-987',
      frentista: null,
      observacao: null,
    })
    expect(criar[0].corpo).not.toHaveProperty('valor')
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('só oferece combustível e posto ativos', async () => {
    const { user } = montar()
    const dialogo = await abrirNovo(user)
    expect(within(dialogo).queryByRole('option', { name: 'Gasolina' })).not.toBeInTheDocument()
    expect(within(dialogo).queryByRole('option', { name: /Posto Rodovia/ })).not.toBeInTheDocument()
  })

  it('um 422 da API aparece no formulário com a mensagem literal', async () => {
    const mensagem = 'A nota fiscal NF-987 já foi lançada para este posto.'
    capturar('post', '/abastecimento', responderErro(422, [mensagem]))
    const { user } = montar()
    const dialogo = await abrirNovo(user)

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1')
    await user.selectOptions(within(dialogo).getByLabelText('Motorista'), 'João Pereira')
    await preencherAbastecimento(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Lançar' }))

    expect(await within(dialogo).findByText(mensagem)).toBeInTheDocument()
    expect(screen.getByRole('dialog', { name: 'Novo abastecimento' })).toBeInTheDocument()
  })

  it('odômetro abaixo da ficha do veículo avisa, sem bloquear', async () => {
    const { user } = montar()
    const dialogo = await abrirNovo(user)
    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D02 — Caminhão 2')
    await user.type(within(dialogo).getByLabelText('Odômetro (km)'), '100000')

    expect(within(dialogo).getByText(/menor que a quilometragem atual do veículo/)).toBeInTheDocument()
    expect(within(dialogo).getByRole('button', { name: 'Lançar' })).toBeEnabled()
  })

  it('estima o km/l desde o abastecimento anterior que o servidor apontou', async () => {
    const { user, anterior } = montar()
    anterior.length = 0
    capturar('get', '/abastecimento/anterior', responderJson({ dataAbastecimento: '2026-11-01T08:00:00', odometro: 50_000 }))
    const dialogo = await abrirNovo(user)

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1')
    await user.type(within(dialogo).getByLabelText('Odômetro (km)'), '50500')
    await user.type(within(dialogo).getByLabelText('Litros'), '48.5')

    expect(await within(dialogo).findByText(porTextoCompleto(/Desde o abastecimento de 01\/11\/2026/))).toHaveTextContent(
      /500 km ÷ 48,5 L ≈ 10,3 km\/l/,
    )
  })

  it('sem abastecimento anterior não há estimativa — e o formulário segue normal', async () => {
    const { user, anterior } = montar()
    const dialogo = await abrirNovo(user)

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1')
    await user.type(within(dialogo).getByLabelText('Odômetro (km)'), '50500')
    await user.type(within(dialogo).getByLabelText('Litros'), '48.5')

    await waitFor(() => expect(anterior.at(-1)?.params).toMatchObject({ veiculoId: '1', odometro: '50500' }))
    expect(within(dialogo).queryByText(/Desde o abastecimento de/)).not.toBeInTheDocument()
  })
})

describe('AbastecimentosPage — motorista', () => {
  it('o campo motorista vem desabilitado com o próprio nome, e a lista de motoristas nem é pedida', async () => {
    const motoristas = capturar('get', '/motorista', responderJson([]))
    const { user } = montar({ papel: 'Motorista' })
    const dialogo = await abrirNovo(user)

    const campo = within(dialogo).getByLabelText('Motorista')
    expect(campo).toBeDisabled()
    expect(campo).toHaveValue('Usuário Motorista')
    expect(motoristas).toHaveLength(0)
  })

  it('o POST não leva motoristaId: a API resolve pelo token', async () => {
    const criar = capturar('post', '/abastecimento', responderJson(fabricarAbastecimento()))
    const { user } = montar({ papel: 'Motorista' })
    const dialogo = await abrirNovo(user)

    await user.selectOptions(within(dialogo).getByLabelText('Veículo'), 'ABC1D01 — Caminhão 1')
    await preencherAbastecimento(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Lançar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).not.toHaveProperty('motoristaId')
    expect(criar[0].corpo).not.toHaveProperty('valor')
  })

  it('com rota ativa, o select de veículo mostra só o da rota, já escolhido', async () => {
    const rota = fabricarRota({ ativo: true, codigoVeiculo: 2, enderecoPartida: 'Joinville', enderecoChegada: 'Curitiba' })
    const { user, minhasRotas } = montar({ papel: 'Motorista', rotaAtiva: rota })
    await waitFor(() => expect(minhasRotas[0]?.params).toMatchObject({ ativo: 'true', tamanhoPagina: '1' }))
    const dialogo = await abrirNovo(user)

    const veiculo = within(dialogo).getByLabelText('Veículo')
    const opcoes = within(veiculo).getAllByRole('option').map((o) => o.textContent)
    expect(opcoes).toEqual(['Selecione…', 'ABC1D02 — Caminhão 2'])
    expect(veiculo).toHaveValue('2')
    expect(within(dialogo).getByText(porTextoCompleto(/Você está em rota com ABC1D02 \(Joinville → Curitiba\)/))).toBeInTheDocument()
  })

  it('sem rota ativa, o motorista escolhe entre a frota inteira', async () => {
    const { user } = montar({ papel: 'Motorista' })
    const dialogo = await abrirNovo(user)
    const opcoes = within(within(dialogo).getByLabelText('Veículo')).getAllByRole('option')
    expect(opcoes).toHaveLength(3)
  })

  it('a gestão não consulta rota ativa', async () => {
    const { minhasRotas } = montar({ papel: 'Operador' })
    await screen.findByText('NF 000123')
    expect(minhasRotas).toHaveLength(0)
  })
})

describe('AbastecimentosPage — correção e exclusão', () => {
  it('na correção veículo e motorista somem e o PUT não os envia, nem o valor', async () => {
    const atualizar = capturar('put', '/abastecimento/:id', responderJson(fabricarAbastecimento()))
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: 'Editar o abastecimento do veículo ABC1D01 de 10/11/2026' }))

    const dialogo = screen.getByRole('dialog', { name: 'Corrigir abastecimento' })
    expect(within(dialogo).queryByLabelText('Veículo')).not.toBeInTheDocument()
    expect(within(dialogo).queryByLabelText('Motorista')).not.toBeInTheDocument()
    expect(within(dialogo).getByLabelText('Litros')).toHaveValue(100)

    await user.clear(within(dialogo).getByLabelText('Litros'))
    await user.type(within(dialogo).getByLabelText('Litros'), '90')
    await user.click(within(dialogo).getByRole('button', { name: 'Salvar correção' }))

    await waitFor(() => expect(atualizar).toHaveLength(1))
    expect(atualizar[0].url.pathname).toMatch(/\/abastecimento\/501$/)
    expect(atualizar[0].corpo).toMatchObject({ litros: 90, valorLitro: 6.5, notaFiscal: '000123', dataAbastecimento: '2026-11-10' })
    for (const campo of ['veiculoId', 'motoristaId', 'valor']) expect(atualizar[0].corpo).not.toHaveProperty(campo)
  })

  it('Admin exclui depois de confirmar', async () => {
    const excluir = capturar('delete', '/abastecimento/:id')
    const { user } = montar({ papel: 'Admin' })
    await user.click(await screen.findByRole('button', { name: 'Excluir o abastecimento do veículo ABC1D02 de 11/11/2026' }))

    const confirmacao = screen.getByRole('alertdialog', { name: 'Excluir abastecimento' })
    await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))

    await waitFor(() => expect(excluir).toHaveLength(1))
    expect(excluir[0].url.pathname).toMatch(/\/abastecimento\/502$/)
    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
  })

  it('um 422 na exclusão aparece literal na confirmação', async () => {
    const mensagem = 'Não é possível excluir um abastecimento de rota encerrada.'
    capturar('delete', '/abastecimento/:id', responderErro(422, [mensagem]))
    const { user } = montar({ papel: 'Supervisor' })
    await user.click(await screen.findByRole('button', { name: /Excluir o abastecimento do veículo ABC1D01/ }))
    const confirmacao = screen.getByRole('alertdialog', { name: 'Excluir abastecimento' })
    await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))

    expect(await within(confirmacao).findByText(mensagem)).toBeInTheDocument()
  })

  it('Operador corrige mas não exclui', async () => {
    montar({ papel: 'Operador' })
    expect(await screen.findAllByRole('button', { name: /Editar o abastecimento/ })).toHaveLength(2)
    expect(screen.queryByRole('button', { name: /Excluir o abastecimento/ })).not.toBeInTheDocument()
  })
})
