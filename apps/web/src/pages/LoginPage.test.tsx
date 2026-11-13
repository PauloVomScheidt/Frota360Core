import { screen, waitFor } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { Route, Routes, type InitialEntry } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { tokenStorage } from '../api/tokenStorage'
import type { Role } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarSessao } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { RotaAtual } from '../test/RotaAtual'
import { LoginPage } from './LoginPage'

function telaDeLogin(rota: InitialEntry = '/login') {
  return renderizar(
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="*" element={<RotaAtual />} />
    </Routes>,
    { rota },
  )
}

async function entrar(user: UserEvent, email = 'ana@frota360.teste', senha = 'Senha123') {
  await user.type(screen.getByLabelText('E-mail'), email)
  await user.type(screen.getByLabelText('Senha'), senha)
  await user.click(screen.getByRole('button', { name: 'Entrar' }))
}

const rotaAtual = () => screen.findByRole('status', { name: 'Rota atual' })

describe('LoginPage — sucesso', () => {
  it.each<[Role, string]>([
    ['Admin', '/dashboard'],
    ['Supervisor', '/dashboard'],
    ['Operador', '/dashboard'],
    ['Motorista', '/minhas-rotas'],
  ])('%s entra e vai para %s, com a sessão gravada', async (role, destino) => {
    const sessao = fabricarSessao({ nome: 'Ana Souza', email: 'ana@frota360.teste', role, nomeEmpresa: 'Frota X' })
    const login = capturar('post', '/auth/login', responderJson(sessao))
    const { user } = telaDeLogin()

    await entrar(user)

    expect(await rotaAtual()).toHaveTextContent(destino)
    expect(login[0].corpo).toEqual({ email: 'ana@frota360.teste', senha: 'Senha123' })
    expect(tokenStorage.getUser()).toEqual({ nome: 'Ana Souza', email: 'ana@frota360.teste', role, nomeEmpresa: 'Frota X' })
  })

  it('volta para a tela de onde o guarda o tirou', async () => {
    capturar('post', '/auth/login', responderJson(fabricarSessao({ role: 'Operador' })))
    const { user } = telaDeLogin({ pathname: '/login', state: { from: '/veiculos' } })

    await entrar(user)

    expect(await rotaAtual()).toHaveTextContent('/veiculos')
  })

  it('limpa o cache da sessão anterior — trocar de conta não mostra dado de outra empresa', async () => {
    capturar('post', '/auth/login', responderJson(fabricarSessao({ role: 'Admin' })))
    const { user, queryClient } = telaDeLogin()
    queryClient.setQueryData(['veiculos'], ['da empresa anterior'])

    await entrar(user)

    await rotaAtual()
    expect(queryClient.getQueryData(['veiculos'])).toBeUndefined()
  })

  it('o botão fica pendente enquanto a API responde', async () => {
    let liberar: () => void = () => {}
    const resposta = new Promise<void>((r) => (liberar = r))
    capturar('post', '/auth/login', async () => {
      await resposta
      return responderJson(fabricarSessao())()
    })
    const { user } = telaDeLogin()

    await entrar(user)
    expect(screen.getByRole('button', { name: 'Entrando…' })).toBeDisabled()
    liberar()
    await rotaAtual()
  })
})

describe('LoginPage — falha', () => {
  it('credencial inválida mostra a mensagem da API, sem tentar refresh e sem gravar sessão', async () => {
    capturar('post', '/auth/login', responderErro(401, ['E-mail ou senha inválidos.'], 'Não autorizado.'))
    const refresh = capturar('post', '/auth/refresh', responderJson(fabricarSessao()))
    const { user } = telaDeLogin()

    await entrar(user, 'ana@frota360.teste', 'errada')

    expect(await screen.findByText('E-mail ou senha inválidos.')).toBeInTheDocument()
    expect(refresh).toHaveLength(0)
    expect(tokenStorage.getUser()).toBeNull()
    expect(screen.queryByRole('status', { name: 'Rota atual' })).not.toBeInTheDocument()
  })

  it('conta bloqueada pelo rate limit (429) também aparece literal', async () => {
    capturar('post', '/auth/login', responderErro(429, ['Muitas tentativas. Aguarde um minuto.']))
    const { user } = telaDeLogin()
    await entrar(user)
    expect(await screen.findByText('Muitas tentativas. Aguarde um minuto.')).toBeInTheDocument()
  })

  it('API fora do ar é dita com todas as letras', async () => {
    capturar('post', '/auth/login', () => Response.error())
    const { user } = telaDeLogin()
    await entrar(user)
    expect(await screen.findByText('Não foi possível conectar à API. Verifique se ela está no ar.')).toBeInTheDocument()
  })
})

describe('LoginPage — campos', () => {
  it('com os campos vazios o formulário não é enviado', async () => {
    const login = capturar('post', '/auth/login', responderJson(fabricarSessao()))
    const { user } = telaDeLogin()

    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(screen.getByLabelText('E-mail')).toBeInvalid()
    expect(screen.getByLabelText('Senha')).toBeInvalid()
    expect(login).toHaveLength(0)
  })

  it('e-mail sem formato de e-mail não é enviado', async () => {
    const login = capturar('post', '/auth/login', responderJson(fabricarSessao()))
    const { user } = telaDeLogin()
    await entrar(user, 'ana', 'Senha123')

    expect(screen.getByLabelText('E-mail')).toBeInvalid()
    expect(login).toHaveLength(0)
  })

  it('o botão de olho mostra e esconde a senha', async () => {
    const { user } = telaDeLogin()
    const senha = screen.getByLabelText('Senha')
    expect(senha).toHaveAttribute('type', 'password')

    await user.click(screen.getByRole('button', { name: 'Mostrar senha' }))
    expect(senha).toHaveAttribute('type', 'text')

    await user.click(screen.getByRole('button', { name: 'Ocultar senha' }))
    expect(senha).toHaveAttribute('type', 'password')
  })

  it('leva para "Esqueci minha senha" e não oferece cadastro — conta nasce por convite', () => {
    telaDeLogin()
    expect(screen.getByRole('link', { name: 'Esqueci minha senha' })).toHaveAttribute('href', '/esqueci-senha')
    expect(screen.getByText(/criado por convite/)).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /cadastr/i })).not.toBeInTheDocument()
  })

  it('aceita digitação no campo de e-mail', async () => {
    const { user } = telaDeLogin()
    await user.type(screen.getByLabelText('E-mail'), 'admin@frota360.teste')
    await waitFor(() => expect(screen.getByLabelText('E-mail')).toHaveValue('admin@frota360.teste'))
  })
})
