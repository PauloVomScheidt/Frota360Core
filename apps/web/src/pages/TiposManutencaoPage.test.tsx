import { screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { capturar, responderJson } from '../test/capturar'
import { fabricarTipoManutencao } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { testarCatalogo } from '../test/suiteCatalogo'
import { TiposManutencaoPage } from './TiposManutencaoPage'

const OLEO = fabricarTipoManutencao({ id: 1, nome: 'Troca de óleo', intervaloKm: 10_000, ativo: true })
const LAVAGEM = fabricarTipoManutencao({ id: 2, nome: 'Lavagem de motor', intervaloKm: null, ativo: false })

testarCatalogo({
  tela: () => <TiposManutencaoPage />,
  titulo: 'Tipos de manutenção',
  recurso: '/tipomanutencao',
  botaoNovo: 'Novo tipo',
  dialogoNovo: 'Novo tipo de manutenção',
  dialogoEdicao: 'Editar tipo de manutenção',
  tituloExclusao: 'Excluir tipo de manutenção',
  textoVazio: /cadastre o primeiro para poder agendar manutenções/,
  itens: [OLEO, LAVAGEM],
  descricao: (t) => `o tipo ${t.nome}`,
  rotuloNome: 'Nome',
  rotuloSituacao: 'Situação',
  situacao: { ativo: 'Ativo', inativo: 'Inativo' },
  alternar: { desativar: 'Inativar', ativar: 'Ativar' },
  preencherNovo: async (user, dialogo) => {
    await user.type(within(dialogo).getByLabelText('Nome'), 'Revisão de freios')
    await user.type(within(dialogo).getByLabelText('Intervalo (km, opcional)'), '30000')
  },
  corpoNovo: { nome: 'Revisão de freios', intervaloKm: 30_000 },
  corpoRenomeado: (t) => ({ nome: 'Renomeado', intervaloKm: t.intervaloKm, ativo: true }),
  corpoInativado: (t) => ({ nome: t.nome, intervaloKm: t.intervaloKm, ativo: false }),
})

describe('Tipos de manutenção — intervalo', () => {
  it('mostra o intervalo formatado, e "—" quando não há', async () => {
    capturar('get', '/tipomanutencao', responderJson([OLEO, LAVAGEM]))
    renderizar(<TiposManutencaoPage />, { papel: 'Admin' })
    expect(within(await screen.findByRole('row', { name: /Troca de óleo/ })).getAllByRole('cell')[1]).toHaveTextContent('10.000 km')
    expect(within(screen.getByRole('row', { name: /Lavagem de motor/ })).getAllByRole('cell')[1]).toHaveTextContent('—')
  })

  it('intervalo em branco vai como null, não como 0', async () => {
    capturar('get', '/tipomanutencao', responderJson([OLEO]))
    const criar = capturar('post', '/tipomanutencao', responderJson(OLEO))
    const { user } = renderizar(<TiposManutencaoPage />, { papel: 'Admin' })
    await user.click(await screen.findByRole('button', { name: 'Novo tipo' }))
    const dialogo = screen.getByRole('dialog', { name: 'Novo tipo de manutenção' })
    await user.type(within(dialogo).getByLabelText('Nome'), 'Alinhamento')
    await user.click(within(dialogo).getByRole('button', { name: 'Cadastrar' }))

    await waitFor(() => expect(criar).toHaveLength(1))
    expect(criar[0].corpo).toEqual({ nome: 'Alinhamento', intervaloKm: null })
  })
})
