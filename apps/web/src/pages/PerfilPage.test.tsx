import { screen, waitFor } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { tokenStorage } from '../api/tokenStorage'
import type { PerfilResponse, Role } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { porTextoCompleto } from '../test/consultas'
import { fabricarPerfil, fabricarSessao } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { PerfilPage } from './PerfilPage'

const PERFIL = fabricarPerfil({
  id: 42,
  nome: 'Ana Souza',
  email: 'ana@frota360.teste',
  role: 'Motorista',
  cpf: '12345678901',
  dataNascimento: '1990-05-20T00:00:00',
  nomeEmpresa: 'Transportes Horizonte Ltda',
  cnpjEmpresa: '12345678000190',
})

function montar(perfil: PerfilResponse = PERFIL, papel: Role = 'Motorista') {
  tokenStorage.setSession(fabricarSessao({ nome: perfil.nome, email: perfil.email, role: papel }))
  const leitura = capturar('get', '/usuario/perfil', responderJson(perfil))
  const tela = renderizar(<PerfilPage />, { rota: '/perfil' })
  return { ...tela, leitura }
}

const campo = (rotulo: string) => screen.getByLabelText(rotulo)

describe('PerfilPage — leitura', () => {
  it('preenche o formulário com o perfil de GET /usuario/perfil', async () => {
    montar()
    await waitFor(() => expect(campo('Nome completo')).toHaveValue('Ana Souza'))
    expect(campo('CPF (opcional)')).toHaveValue('123.456.789-01')
    expect(campo('Data de nascimento (opcional)')).toHaveValue('1990-05-20')
  })

  it('e-mail, permissão e empresa aparecem como texto, não como campo', async () => {
    montar()
    expect(await screen.findByText(porTextoCompleto(/^E-mail: ana@frota360\.teste — é a chave de acesso/))).toBeInTheDocument()
    expect(screen.getByText(porTextoCompleto(/^Permissão: Motorista — concedida pelo administrador/))).toBeInTheDocument()
    expect(
      await screen.findByText(porTextoCompleto(/^Empresa: Transportes Horizonte Ltda · CNPJ 12\.345\.678\/0001-90 — a conta/)),
    ).toBeInTheDocument()
    expect(screen.queryByLabelText(/E-mail/)).not.toBeInTheDocument()
  })

  it('falha ao carregar o perfil aparece na tela', async () => {
    tokenStorage.setSession(fabricarSessao({ role: 'Operador' }))
    capturar('get', '/usuario/perfil', responderErro(500, ['Erro interno.']))
    renderizar(<PerfilPage />)
    expect(await screen.findByText('Erro interno.')).toBeInTheDocument()
  })
})

describe('PerfilPage — salvar', () => {
  it('o PUT leva nome sem espaços, CPF só com dígitos e a data, e o header passa a mostrar o nome novo', async () => {
    const salvar = capturar('put', '/usuario/perfil', ({ corpo }) => responderJson({ ...PERFIL, ...corpo })())
    const { user } = montar()
    await waitFor(() => expect(campo('Nome completo')).toHaveValue('Ana Souza'))

    await user.clear(campo('Nome completo'))
    await user.type(campo('Nome completo'), '  Ana Souza Lima  ')
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    expect(await screen.findByText('Perfil atualizado com sucesso.')).toBeInTheDocument()
    expect(salvar[0].corpo).toEqual({ nome: 'Ana Souza Lima', cpf: '12345678901', dataNascimento: '1990-05-20' })
    expect(tokenStorage.getUser()?.nome).toBe('Ana Souza Lima')
    expect(screen.getByRole('link', { name: /Ana Souza Lima/ })).toHaveAttribute('href', '/perfil')
  })

  it('apagar o CPF o remove do cadastro: o campo sai do corpo', async () => {
    const salvar = capturar('put', '/usuario/perfil', responderJson({ ...PERFIL, cpf: null }))
    const { user } = montar()
    await waitFor(() => expect(campo('CPF (opcional)')).toHaveValue('123.456.789-01'))

    await user.clear(campo('CPF (opcional)'))
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    await waitFor(() => expect(salvar).toHaveLength(1))
    expect(salvar[0].corpo).not.toHaveProperty('cpf')
  })

  it.each([
    ['nome curto', 'Nome completo', 'A', 'Informe seu nome completo.'],
    ['CPF incompleto', 'CPF (opcional)', '1234567890', 'O CPF deve ter 11 dígitos.'],
  ])('%s é recusado no cliente', async (_, rotulo, valor, mensagem) => {
    const salvar = capturar('put', '/usuario/perfil', responderJson(PERFIL))
    const { user } = montar()
    await waitFor(() => expect(campo('Nome completo')).toHaveValue('Ana Souza'))

    await user.clear(campo(rotulo))
    await user.type(campo(rotulo), valor)
    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    expect(screen.getByText(mensagem)).toBeInTheDocument()
    expect(salvar).toHaveLength(0)
  })

  it('CPF já usado por outra pessoa (422) aparece com a mensagem literal', async () => {
    capturar('put', '/usuario/perfil', responderErro(422, ['Este CPF já está cadastrado para outro usuário da empresa.']))
    const { user } = montar()
    await waitFor(() => expect(campo('Nome completo')).toHaveValue('Ana Souza'))

    await user.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    expect(await screen.findByText('Este CPF já está cadastrado para outro usuário da empresa.')).toBeInTheDocument()
    expect(screen.queryByText('Perfil atualizado com sucesso.')).not.toBeInTheDocument()
  })

  it('perfil sem CPF nem nascimento abre com os campos vazios', async () => {
    montar(fabricarPerfil({ id: 7, nome: 'Bruno Lima', cpf: null, dataNascimento: null, cnpjEmpresa: null }), 'Admin')
    await waitFor(() => expect(campo('Nome completo')).toHaveValue('Bruno Lima'))
    expect(campo('CPF (opcional)')).toHaveValue('')
    expect(screen.queryByText(/CNPJ/)).not.toBeInTheDocument()
  })
})
