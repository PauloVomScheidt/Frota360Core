import { screen } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { HttpResponse } from 'msw'
import { Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { capturar, responderErro } from '../test/capturar'
import { renderizar } from '../test/renderizar'
import { RotaAtual } from '../test/RotaAtual'
import { ResetPasswordPage } from './ResetPasswordPage'

const sucesso = () => HttpResponse.json({ sucesso: true, mensagem: 'Senha redefinida.', dados: null, erros: null })

function telaDeRedefinicao(rota = '/redefinir-senha?token=tok-123') {
  return renderizar(
    <Routes>
      <Route path="/redefinir-senha" element={<ResetPasswordPage />} />
      <Route path="*" element={<RotaAtual />} />
    </Routes>,
    { rota },
  )
}

async function redefinir(user: UserEvent, senha: string, confirmacao = senha) {
  await user.type(screen.getByLabelText('Nova senha'), senha)
  await user.type(screen.getByLabelText('Confirmar nova senha'), confirmacao)
  await user.click(screen.getByRole('button', { name: 'Redefinir senha' }))
}

describe('ResetPasswordPage — token', () => {
  it('sem token na URL, diz que o link é inválido e oferece pedir outro', () => {
    telaDeRedefinicao('/redefinir-senha')
    expect(screen.getByRole('heading', { name: 'Link inválido' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Solicitar novo link' })).toHaveAttribute('href', '/esqueci-senha')
    expect(screen.queryByLabelText('Nova senha')).not.toBeInTheDocument()
  })

  it('token inválido ou expirado: a recusa da API aparece literal, sem refresh de sessão', async () => {
    capturar('post', '/auth/redefinir-senha', responderErro(400, ['Token inválido ou expirado.']))
    const refresh = capturar('post', '/auth/refresh', sucesso)
    const { user } = telaDeRedefinicao()

    await redefinir(user, 'NovaSenha1')

    expect(await screen.findByText('Token inválido ou expirado.')).toBeInTheDocument()
    expect(refresh).toHaveLength(0)
  })
})

describe('ResetPasswordPage — regras de senha (auth/senha.ts)', () => {
  it.each([
    ['Ab1', 'A senha deve ter no mínimo 6 caracteres.'],
    ['senha12', 'A senha deve conter ao menos 1 letra maiúscula.'],
    ['SenhaForte', 'A senha deve conter ao menos 1 número.'],
  ])('"%s" é recusada no cliente: %s', async (senha, mensagem) => {
    const pedido = capturar('post', '/auth/redefinir-senha', sucesso)
    const { user } = telaDeRedefinicao()

    await redefinir(user, senha)

    expect(screen.getByText(mensagem)).toBeInTheDocument()
    expect(pedido).toHaveLength(0)
  })

  it('confirmação diferente é recusada no cliente', async () => {
    const pedido = capturar('post', '/auth/redefinir-senha', sucesso)
    const { user } = telaDeRedefinicao()
    await redefinir(user, 'NovaSenha1', 'NovaSenha2')

    expect(screen.getByText('As senhas não conferem.')).toBeInTheDocument()
    expect(pedido).toHaveLength(0)
  })
})

describe('ResetPasswordPage — sucesso', () => {
  it('envia token e senha, avisa que as sessões caíram e leva ao login', async () => {
    const pedido = capturar('post', '/auth/redefinir-senha', sucesso)
    const { user } = telaDeRedefinicao()

    await redefinir(user, 'NovaSenha1')

    expect(await screen.findByRole('heading', { name: 'Senha redefinida' })).toBeInTheDocument()
    expect(screen.getByText(/as sessões antigas foram encerradas/)).toBeInTheDocument()
    expect(pedido[0].corpo).toEqual({ token: 'tok-123', novaSenha: 'NovaSenha1' })

    // O redirecionamento é automático, depois de 2,5 s.
    expect(await screen.findByRole('status', { name: 'Rota atual' }, { timeout: 4000 })).toHaveTextContent('/login')
  })
})
