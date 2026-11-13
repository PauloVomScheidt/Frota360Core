import { screen, waitFor, within } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import type { Role, VeiculoResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarVeiculo } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { VeiculosPage } from './VeiculosPage'

const BAU = fabricarVeiculo({
  id: 1,
  nomeVeiculo: 'Caminhão Baú',
  marcaVeiculo: 'Volvo',
  placa: 'ABC1D01',
  quilometragem: 50_000,
  emRota: true,
  ultimoMotorista: 'João Pereira',
  dataUltimaViagem: '2026-11-10T00:00:00',
})
const TRUCK = fabricarVeiculo({ id: 2, nomeVeiculo: 'Truck', marcaVeiculo: 'Scania', placa: 'XYZ9K88', emRota: false })

function montar(papel: Role = 'Admin', veiculos: VeiculoResponse[] = [BAU, TRUCK]) {
  const lista = capturar('get', '/veiculo', responderJson(veiculos))
  return { ...renderizar(<VeiculosPage />, { rota: '/veiculos', papel }), lista }
}

const linha = (texto: string) => screen.findByRole('row', { name: new RegExp(texto) })

async function preencher(user: UserEvent, dialogo: HTMLElement) {
  await user.type(within(dialogo).getByLabelText('Nome do veículo'), 'Van')
  await user.type(within(dialogo).getByLabelText('Marca'), 'Fiat')
  await user.type(within(dialogo).getByLabelText('Placa'), 'qwe1r23')
  await user.type(within(dialogo).getByLabelText('Quilometragem'), '1200')
}

describe('VeiculosPage — listagem', () => {
  it('mostra título, situação derivada da API e o último motorista', async () => {
    montar()
    expect(screen.getByRole('heading', { name: 'Veículos' })).toBeInTheDocument()
    const bau = within(await linha('Caminhão Baú')).getAllByRole('cell')
    expect(bau[3]).toHaveTextContent('Em rota')
    expect(bau[4]).toHaveTextContent('50.000 km')
    expect(bau[5]).toHaveTextContent('João Pereira')
    expect(within(await linha('Truck')).getAllByRole('cell')[3]).toHaveTextContent('Disponível')
  })

  it('frota vazia', async () => {
    montar('Admin', [])
    expect(await screen.findByText('Nenhum veículo cadastrado ainda.')).toBeInTheDocument()
  })

  it.each<[Role, boolean, boolean]>([
    ['Admin', true, true],
    ['Supervisor', true, true],
    ['Operador', false, false],
    ['Motorista', false, false],
  ])('%s: editar=%s, excluir=%s', async (papel, editar, excluir) => {
    montar(papel)
    await linha('Truck')
    expect(screen.queryAllByRole('button', { name: /^Editar o veículo/ })).toHaveLength(editar ? 2 : 0)
    expect(screen.queryAllByRole('button', { name: /^Excluir o veículo/ })).toHaveLength(excluir ? 2 : 0)
    expect(!!screen.queryByRole('button', { name: 'Novo veículo' })).toBe(editar)
  })
})

describe('VeiculosPage — cadastro e edição', () => {
  it('cadastra com a placa em maiúsculas e sem histórico de viagem', async () => {
    const criar = capturar('post', '/veiculo', responderJson(TRUCK))
    const { user, lista } = montar()
    await user.click(await screen.findByRole('button', { name: 'Novo veículo' }))
    const dialogo = screen.getByRole('dialog', { name: 'Novo veículo' })

    await preencher(user, dialogo)
    expect(within(dialogo).getByLabelText('Placa')).toHaveValue('QWE1R23')
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toEqual({
      nomeVeiculo: 'Van',
      marcaVeiculo: 'Fiat',
      placa: 'QWE1R23',
      quilometragem: 1200,
      ultimoMotorista: null,
      dataUltimaViagem: null,
    })
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('a edição reenvia último motorista e última viagem — o PUT substitui o registro inteiro', async () => {
    const atualizar = capturar('put', '/veiculo/:id', responderJson(BAU))
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: 'Editar o veículo ABC1D01' }))
    const dialogo = screen.getByRole('dialog', { name: 'Editar veículo' })

    await user.clear(within(dialogo).getByLabelText('Quilometragem'))
    await user.type(within(dialogo).getByLabelText('Quilometragem'), '51000')
    await user.click(within(dialogo).getByRole('button', { name: 'Salvar alterações' }))

    await waitFor(() => expect(atualizar).toHaveLength(1))
    expect(atualizar[0].url.pathname).toMatch(/\/veiculo\/1$/)
    expect(atualizar[0].corpo).toEqual({
      nomeVeiculo: 'Caminhão Baú',
      marcaVeiculo: 'Volvo',
      placa: 'ABC1D01',
      quilometragem: 51_000,
      ultimoMotorista: 'João Pereira',
      dataUltimaViagem: '2026-11-10T00:00:00',
    })
  })

  it('placa duplicada (422) aparece literal', async () => {
    capturar('post', '/veiculo', responderErro(422, ['Já existe um veículo com a placa QWE1R23.']))
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: 'Novo veículo' }))
    const dialogo = screen.getByRole('dialog', { name: 'Novo veículo' })
    await preencher(user, dialogo)
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))
    expect(await within(dialogo).findByText('Já existe um veículo com a placa QWE1R23.')).toBeInTheDocument()
  })
})

describe('VeiculosPage — exclusão', () => {
  it('exclui depois de confirmar', async () => {
    const excluir = capturar('delete', '/veiculo/:id')
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: 'Excluir o veículo XYZ9K88' }))
    await user.click(within(screen.getByRole('alertdialog', { name: 'Excluir veículo' })).getByRole('button', { name: 'Excluir' }))
    await waitFor(() => expect(excluir).toHaveLength(1))
    expect(excluir[0].url.pathname).toMatch(/\/veiculo\/2$/)
  })

  it('veículo com rotas (422) não sai, e o motivo aparece no diálogo', async () => {
    const mensagem = 'Não é possível excluir um veículo com rotas associadas.'
    capturar('delete', '/veiculo/:id', responderErro(422, [mensagem]))
    const { user } = montar()
    await user.click(await screen.findByRole('button', { name: 'Excluir o veículo ABC1D01' }))
    const confirmacao = screen.getByRole('alertdialog')
    await user.click(within(confirmacao).getByRole('button', { name: 'Excluir' }))
    expect(await within(confirmacao).findByText(mensagem)).toBeInTheDocument()
  })
})
