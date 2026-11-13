import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { capturar, responderJson } from '../test/capturar'
import { fabricarPosto } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { testarCatalogo } from '../test/suiteCatalogo'
import { PostosPage } from './PostosPage'

const CENTRAL = fabricarPosto({ id: 1, nome: 'Posto Central', cnpj: '12.345.678/0001-90', cidade: 'Joinville', ativo: true })
const RODOVIA = fabricarPosto({ id: 2, nome: 'Posto Rodovia', cnpj: null, cidade: null, ativo: false })

testarCatalogo({
  tela: () => <PostosPage />,
  titulo: 'Postos',
  recurso: '/posto',
  botaoNovo: 'Novo posto',
  dialogoNovo: 'Novo posto',
  dialogoEdicao: 'Editar posto',
  tituloExclusao: 'Excluir posto',
  textoVazio: /Nenhum posto credenciado ainda/,
  itens: [CENTRAL, RODOVIA],
  descricao: (p) => `o posto ${p.nome}`,
  rotuloNome: 'Nome',
  rotuloSituacao: 'Credenciamento',
  situacao: { ativo: 'Credenciado', inativo: 'Descredenciado' },
  alternar: { desativar: 'Descredenciar', ativar: 'Credenciar' },
  preencherNovo: async (user, dialogo) => {
    await user.type(within(dialogo).getByLabelText('Nome'), '  Posto Novo  ')
    await user.type(within(dialogo).getByLabelText('Cidade'), 'Blumenau')
  },
  corpoNovo: { nome: 'Posto Novo', cnpj: null, cidade: 'Blumenau' },
  corpoRenomeado: (p) => ({ nome: 'Renomeado', cnpj: p.cnpj, cidade: p.cidade, ativo: true }),
  corpoInativado: (p) => ({ nome: p.nome, cnpj: p.cnpj, cidade: p.cidade, ativo: false }),
})

describe('Postos — campos próprios', () => {
  it('CNPJ e cidade ausentes aparecem como "—"', async () => {
    capturar('get', '/posto', responderJson([RODOVIA]))
    renderizar(<PostosPage />, { papel: 'Admin' })
    const celulas = within(await screen.findByRole('row', { name: /Posto Rodovia/ })).getAllByRole('cell')
    expect(celulas[1]).toHaveTextContent('—')
    expect(celulas[2]).toHaveTextContent('—')
  })
})
