import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarMotorista } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { MotoristasPage } from './MotoristasPage'

describe('MotoristasPage', () => {
  it('lista os motoristas, com CPF formatado e "—" para o que não foi informado', async () => {
    capturar(
      'get',
      '/motorista',
      responderJson([
        fabricarMotorista({ nome: 'João Pereira', cpf: '12345678901', dataNascimento: '1985-03-02T00:00:00' }),
        fabricarMotorista({ nome: 'Carla Souza', cpf: null, dataNascimento: null, ativo: false }),
      ]),
    )
    renderizar(<MotoristasPage />, { papel: 'Operador' })

    expect(screen.getByRole('heading', { name: 'Motoristas' })).toBeInTheDocument()
    const joao = within(await screen.findByRole('row', { name: /João Pereira/ })).getAllByRole('cell')
    expect(joao[2]).toHaveTextContent('123.456.789-01')
    expect(joao[3]).toHaveTextContent('02/03/1985')
    expect(joao[4]).toHaveTextContent('Ativo')
    const carla = within(screen.getByRole('row', { name: /Carla Souza/ })).getAllByRole('cell')
    expect(carla[2]).toHaveTextContent('—')
    expect(carla[4]).toHaveTextContent('Inativo')
  })

  it('é somente leitura: não há cadastro, edição nem exclusão', async () => {
    renderizar(<MotoristasPage />, { papel: 'Admin' })
    await screen.findByRole('row', { name: /João Pereira/ })
    expect(screen.queryByRole('button', { name: /Novo|Editar|Excluir/ })).not.toBeInTheDocument()
  })

  it('sem motorista, diz isso', async () => {
    capturar('get', '/motorista', responderJson([]))
    renderizar(<MotoristasPage />, { papel: 'Admin' })
    expect(await screen.findByText('Nenhum usuário com o perfil Motorista ainda.')).toBeInTheDocument()
  })

  it('falha ao carregar aparece na tabela', async () => {
    capturar('get', '/motorista', responderErro(500, ['Erro interno.']))
    renderizar(<MotoristasPage />, { papel: 'Admin' })
    expect(await screen.findByText(/Erro interno\.|Não foi possível carregar os motoristas\./)).toBeInTheDocument()
  })

  it('pagina no cliente quando a lista passa de 10', async () => {
    capturar('get', '/motorista', responderJson(Array.from({ length: 18 }, (_, i) => fabricarMotorista({ nome: `Pessoa ${i + 1}` }))))
    const { user } = renderizar(<MotoristasPage />, { papel: 'Admin' })
    expect(await screen.findByText('1–15 de 18')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    expect(screen.getByText('16–18 de 18')).toBeInTheDocument()
    expect(screen.getByRole('row', { name: /Pessoa 18/ })).toBeInTheDocument()
  })
})
