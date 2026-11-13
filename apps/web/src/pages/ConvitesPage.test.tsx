import { screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { ConviteResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { renderizar } from '../test/renderizar'
import { ConvitesPage } from './ConvitesPage'

function convite(sobrescrita: Partial<ConviteResponse>): ConviteResponse {
  return {
    id: 1,
    email: 'pessoa@frota360.teste',
    role: 'Operador',
    expiraEm: '2026-11-19T10:00:00',
    utilizadoEm: null,
    dataInclusao: '2026-11-12T10:00:00',
    linkConvite: null,
    ...sobrescrita,
  }
}

const PENDENTE = convite({ id: 1, email: 'pendente@frota360.teste' })
const UTILIZADO = convite({ id: 2, email: 'aceito@frota360.teste', role: 'Motorista', utilizadoEm: '2026-11-13T08:30:00' })
const EXPIRADO = convite({ id: 3, email: 'vencido@frota360.teste', expiraEm: '2026-11-01T10:00:00' })

function montar(convites: ConviteResponse[] = [PENDENTE, UTILIZADO, EXPIRADO]) {
  const lista = capturar('get', '/convite', responderJson(convites))
  return { ...renderizar(<ConvitesPage />, { rota: '/convites', papel: 'Admin' }), lista }
}

const linha = (email: string) => screen.findByRole('row', { name: new RegExp(email) })

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date(2026, 10, 14, 12, 0))
})
afterEach(() => vi.useRealTimers())

describe('ConvitesPage — listagem', () => {
  it.each([
    ['pendente@frota360.teste', 'Pendente'],
    ['aceito@frota360.teste', 'Utilizado'],
    ['vencido@frota360.teste', 'Expirado'],
  ])('%s aparece como %s — status derivado no cliente', async (email, status) => {
    montar()
    expect(within(await linha(email)).getAllByRole('cell')[2]).toHaveTextContent(status)
  })

  it('convite utilizado mostra quando foi aceito, no lugar do cancelar', async () => {
    montar()
    const aceito = await linha('aceito@frota360.teste')
    expect(aceito).toHaveTextContent('Aceito em 13/11/2026, 08:30:00')
    expect(within(aceito).queryByRole('button', { name: 'Cancelar' })).not.toBeInTheDocument()
  })

  it('nenhum convite enviado', async () => {
    montar([])
    expect(await screen.findByText('Nenhum convite enviado ainda.')).toBeInTheDocument()
  })
})

describe('ConvitesPage — novo convite', () => {
  it('envia e-mail e permissão, e mostra o link em claro fora do modal', async () => {
    const criar = capturar('post', '/convite', responderJson(convite({ id: 9, linkConvite: 'http://localhost:5173/convite?token=abc' })))
    const { user, lista } = montar()
    await user.click(screen.getByRole('button', { name: 'Novo convite' }))
    const dialogo = screen.getByRole('dialog', { name: 'Novo convite' })

    expect(within(dialogo).getByLabelText('Permissão')).toHaveValue('Operador')
    await user.type(within(dialogo).getByLabelText('E-mail do convidado'), 'nova@frota360.teste')
    await user.selectOptions(within(dialogo).getByLabelText('Permissão'), 'Motorista')
    expect(dialogo).toHaveTextContent('Abre e encerra as próprias rotas')
    await user.click(within(dialogo).getByRole('button', { name: 'Enviar convite' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toEqual({ email: 'nova@frota360.teste', role: 'Motorista' })
    expect(await screen.findByText('http://localhost:5173/convite?token=abc')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    await waitFor(() => expect(lista.length).toBeGreaterThan(1))
  })

  it('"Copiar link" copia para a área de transferência', async () => {
    capturar('post', '/convite', responderJson(convite({ linkConvite: 'http://localhost:5173/convite?token=xyz' })))
    const { user } = montar()
    await user.click(screen.getByRole('button', { name: 'Novo convite' }))
    await user.type(screen.getByLabelText('E-mail do convidado'), 'nova@frota360.teste')
    await user.click(screen.getByRole('button', { name: 'Enviar convite' }))

    await user.click(await screen.findByRole('button', { name: 'Copiar link' }))
    expect(screen.getByRole('button', { name: 'Copiado!' })).toBeInTheDocument()
    expect(await navigator.clipboard.readText()).toBe('http://localhost:5173/convite?token=xyz')
  })

  it('e-mail já da empresa (422) aparece literal no modal', async () => {
    capturar('post', '/convite', responderErro(422, ['Este e-mail já pertence a um usuário da empresa.']))
    const { user } = montar()
    await user.click(screen.getByRole('button', { name: 'Novo convite' }))
    const dialogo = screen.getByRole('dialog')
    await user.type(within(dialogo).getByLabelText('E-mail do convidado'), 'existe@frota360.teste')
    await user.click(within(dialogo).getByRole('button', { name: 'Enviar convite' }))
    expect(await within(dialogo).findByText('Este e-mail já pertence a um usuário da empresa.')).toBeInTheDocument()
  })
})

describe('ConvitesPage — cancelar', () => {
  it('cancela o convite pendente pelo DELETE', async () => {
    const cancelar = capturar('delete', '/convite/:id')
    const { user } = montar()
    await user.click(within(await linha('pendente@frota360.teste')).getByRole('button', { name: 'Cancelar' }))
    await waitFor(() => expect(cancelar).toHaveLength(1))
    expect(cancelar[0].url.pathname).toMatch(/\/convite\/1$/)
  })

  it('falha ao cancelar aparece na página', async () => {
    capturar('delete', '/convite/:id', responderErro(422, ['Convite já utilizado não pode ser cancelado.']))
    const { user } = montar()
    await user.click(within(await linha('vencido@frota360.teste')).getByRole('button', { name: 'Cancelar' }))
    expect(await screen.findByText('Convite já utilizado não pode ser cancelado.')).toBeInTheDocument()
  })
})
