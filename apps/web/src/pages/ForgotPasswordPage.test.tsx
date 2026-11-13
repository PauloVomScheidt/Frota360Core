import { screen, waitFor } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { capturar, responderErro } from '../test/capturar'
import { renderizar } from '../test/renderizar'
import { ForgotPasswordPage } from './ForgotPasswordPage'

const NEUTRA = 'Se o e-mail estiver cadastrado, você receberá um link para redefinir a senha.'

/** O que a API faz: 200 com a mesma mensagem, exista o e-mail ou não. */
const respostaNeutra = (mensagem = NEUTRA) => () =>
  HttpResponse.json({ sucesso: true, mensagem, dados: null, erros: null })

async function pedirLink(user: UserEvent, email: string) {
  await user.clear(screen.getByLabelText('E-mail corporativo'))
  await user.type(screen.getByLabelText('E-mail corporativo'), email)
  await user.click(screen.getByRole('button', { name: 'Enviar link de redefinição' }))
}

describe('ForgotPasswordPage', () => {
  it('envia o e-mail e mostra a mensagem neutra da API', async () => {
    const pedido = capturar('post', '/auth/esqueci-senha', respostaNeutra())
    const { user } = renderizar(<ForgotPasswordPage />, { rota: '/esqueci-senha' })

    await pedirLink(user, 'ana@frota360.teste')

    expect(await screen.findByText(NEUTRA)).toBeInTheDocument()
    expect(pedido[0].corpo).toEqual({ email: 'ana@frota360.teste' })
  })

  it('a tela é a mesma para e-mail cadastrado e inexistente — não revela qual é qual', async () => {
    capturar('post', '/auth/esqueci-senha', respostaNeutra())
    const { user, container } = renderizar(<ForgotPasswordPage />)

    await pedirLink(user, 'existe@frota360.teste')
    await screen.findByText(NEUTRA)
    const depoisDoExistente = container.textContent?.replace('existe@frota360.teste', '')

    await pedirLink(user, 'naoexiste@frota360.teste')
    await waitFor(() => expect(screen.getByRole('button', { name: 'Enviar link de redefinição' })).toBeEnabled())
    expect(container.textContent?.replace('naoexiste@frota360.teste', '')).toBe(depoisDoExistente)
    expect(screen.queryByText(/não encontrado|não existe|não cadastrado/i)).not.toBeInTheDocument()
  })

  it('sem mensagem da API, usa o texto padrão — também neutro', async () => {
    capturar('post', '/auth/esqueci-senha', respostaNeutra(''))
    const { user } = renderizar(<ForgotPasswordPage />)
    await pedirLink(user, 'ana@frota360.teste')
    expect(
      await screen.findByText('Se o e-mail estiver cadastrado, enviamos um link de redefinição. O link vale por 30 minutos.'),
    ).toBeInTheDocument()
  })

  it('o rate limit (429) aparece com a mensagem da API', async () => {
    capturar('post', '/auth/esqueci-senha', responderErro(429, ['Muitas solicitações. Tente mais tarde.']))
    const { user } = renderizar(<ForgotPasswordPage />)
    await pedirLink(user, 'ana@frota360.teste')
    expect(await screen.findByText('Muitas solicitações. Tente mais tarde.')).toBeInTheDocument()
  })

  it('e-mail vazio não é enviado', async () => {
    const pedido = capturar('post', '/auth/esqueci-senha', respostaNeutra())
    const { user } = renderizar(<ForgotPasswordPage />)
    await user.click(screen.getByRole('button', { name: 'Enviar link de redefinição' }))
    expect(screen.getByLabelText('E-mail corporativo')).toBeInvalid()
    expect(pedido).toHaveLength(0)
  })

  it('tem o caminho de volta para o login', () => {
    renderizar(<ForgotPasswordPage />)
    expect(screen.getByRole('link', { name: 'Voltar para login' })).toHaveAttribute('href', '/login')
  })
})
