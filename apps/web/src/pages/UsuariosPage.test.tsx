import { screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { UsuarioResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarUsuario } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { UsuariosPage } from './UsuariosPage'

// A sessão do Admin da fábrica tem este e-mail — é o "(você)".
const EU = fabricarUsuario({ id: 1, nome: 'Usuário Admin', email: 'admin@frota360.teste', role: 'Admin' })
const OPERADOR = fabricarUsuario({ id: 2, nome: 'Bruno Lima', email: 'bruno@frota360.teste', role: 'Operador' })
const MOTORISTA = fabricarUsuario({ id: 3, nome: 'João Pereira', email: 'joao@frota360.teste', role: 'Motorista' })
const INATIVA = fabricarUsuario({ id: 4, nome: 'Carla Souza', email: 'carla@frota360.teste', role: 'Supervisor', ativo: false })

function montar(usuarios: UsuarioResponse[] = [EU, OPERADOR, MOTORISTA, INATIVA]) {
  const lista = capturar('get', '/usuario', responderJson(usuarios))
  return { ...renderizar(<UsuariosPage />, { rota: '/usuarios', papel: 'Admin' }), lista }
}

const linha = (nome: string) => screen.findByRole('row', { name: new RegExp(nome) })

describe('UsuariosPage — listagem', () => {
  it('mostra a equipe com permissão e status, e marca a própria conta', async () => {
    montar()
    expect(screen.getByRole('heading', { name: 'Usuários' })).toBeInTheDocument()
    expect(within(await linha('Bruno Lima')).getByRole('combobox')).toHaveValue('Operador')
    expect(await linha('Carla Souza')).toHaveTextContent('Inativo')
    expect(await linha('Usuário Admin')).toHaveTextContent('(você)')
  })

  it('a própria conta não pode ser alterada por aqui', async () => {
    montar()
    const eu = await linha('Usuário Admin')
    expect(within(eu).getByRole('combobox')).toBeDisabled()
    expect(within(eu).getByRole('button', { name: 'Desativar' })).toBeDisabled()
  })

  it('empresa sem usuários', async () => {
    montar([])
    expect(await screen.findByText('Nenhum usuário cadastrado.')).toBeInTheDocument()
  })
})

describe('UsuariosPage — trocar permissão', () => {
  it('pede confirmação dizendo o que muda, e só então faz o PUT', async () => {
    const trocar = capturar('put', '/usuario/:id/role')
    const { user, lista } = montar()
    await user.selectOptions(within(await linha('Bruno Lima')).getByRole('combobox'), 'Motorista')

    const confirmacao = screen.getByRole('alertdialog', { name: 'Alterar permissão' })
    expect(confirmacao).toHaveTextContent('Bruno Lima passa de Operador para Motorista.')
    expect(confirmacao).toHaveTextContent('A sessão dele será encerrada')
    expect(confirmacao).toHaveTextContent('passa a enxergar apenas as próprias rotas')
    expect(trocar).toHaveLength(0)

    await user.click(within(confirmacao).getByRole('button', { name: 'Alterar para Motorista' }))

    await waitFor(() => expect(trocar).toHaveLength(1))
    expect(trocar[0].url.pathname).toMatch(/\/usuario\/2\/role$/)
    expect(trocar[0].corpo).toEqual({ role: 'Motorista' })
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('rebaixar um motorista avisa que ele sai da lista de motoristas', async () => {
    const { user } = montar()
    await user.selectOptions(within(await linha('João Pereira')).getByRole('combobox'), 'Operador')
    expect(screen.getByRole('alertdialog')).toHaveTextContent('sai da lista de motoristas')
  })

  it('cancelar não muda nada — o select volta ao papel atual', async () => {
    const trocar = capturar('put', '/usuario/:id/role')
    const { user } = montar()
    const select = within(await linha('Bruno Lima')).getByRole('combobox')
    await user.selectOptions(select, 'Admin')
    await user.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Cancelar' }))

    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
    expect(select).toHaveValue('Operador')
    expect(trocar).toHaveLength(0)
  })

  it('rebaixar o último admin (422) mostra a regra dentro do diálogo', async () => {
    const mensagem = 'A empresa precisa ter ao menos um administrador ativo.'
    capturar('put', '/usuario/:id/role', responderErro(422, [mensagem]))
    const outroAdmin = fabricarUsuario({ id: 9, nome: 'Outra Admin', role: 'Admin' })
    const { user } = montar([EU, outroAdmin])
    await user.selectOptions(within(await linha('Outra Admin')).getByRole('combobox'), 'Supervisor')
    const confirmacao = screen.getByRole('alertdialog')
    await user.click(within(confirmacao).getByRole('button', { name: 'Alterar para Supervisor' }))

    expect(await within(confirmacao).findByText(mensagem)).toBeInTheDocument()
  })
})

describe('UsuariosPage — ativar e desativar', () => {
  it('desativar pede confirmação e lembra que nada é apagado', async () => {
    const status = capturar('put', '/usuario/:id/ativo')
    const { user } = montar()
    await user.click(within(await linha('João Pereira')).getByRole('button', { name: 'Desativar' }))

    const confirmacao = screen.getByRole('alertdialog', { name: 'Desativar usuário' })
    expect(confirmacao).toHaveTextContent('Nada é apagado')
    expect(confirmacao).toHaveTextContent('Rotas em andamento continuam abertas')
    await user.click(within(confirmacao).getByRole('button', { name: 'Desativar' }))

    await waitFor(() => expect(status).toHaveLength(1))
    expect(status[0].url.pathname).toMatch(/\/usuario\/3\/ativo$/)
    expect(status[0].corpo).toEqual({ ativo: false })
  })

  it('reativar vai direto, sem diálogo', async () => {
    const status = capturar('put', '/usuario/:id/ativo')
    const { user } = montar()
    await user.click(within(await linha('Carla Souza')).getByRole('button', { name: 'Ativar' }))

    await waitFor(() => expect(status).toHaveLength(1))
    expect(status[0].corpo).toEqual({ ativo: true })
    expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
  })

  it('falha ao reativar aparece no topo da página', async () => {
    capturar('put', '/usuario/:id/ativo', responderErro(422, ['Usuário bloqueado pelo suporte.']))
    const { user } = montar()
    await user.click(within(await linha('Carla Souza')).getByRole('button', { name: 'Ativar' }))
    expect(await screen.findByText('Usuário bloqueado pelo suporte.')).toBeInTheDocument()
  })

  it('falha ao desativar fica dentro do diálogo', async () => {
    capturar('put', '/usuario/:id/ativo', responderErro(422, ['A empresa precisa ter ao menos um administrador ativo.']))
    const { user } = montar()
    await user.click(within(await linha('Bruno Lima')).getByRole('button', { name: 'Desativar' }))
    const confirmacao = screen.getByRole('alertdialog')
    await user.click(within(confirmacao).getByRole('button', { name: 'Desativar' }))
    expect(await within(confirmacao).findByText('A empresa precisa ter ao menos um administrador ativo.')).toBeInTheDocument()
  })
})
