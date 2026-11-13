import { within } from '@testing-library/react'
import { fabricarTipoDespesa } from '../test/fabricas'
import { testarCatalogo } from '../test/suiteCatalogo'
import { TiposDespesaPage } from './TiposDespesaPage'

testarCatalogo({
  tela: () => <TiposDespesaPage />,
  titulo: 'Tipos de despesa',
  recurso: '/tipodespesa',
  botaoNovo: 'Novo tipo',
  dialogoNovo: 'Novo tipo de despesa',
  dialogoEdicao: 'Editar tipo de despesa',
  tituloExclusao: 'Excluir tipo de despesa',
  textoVazio: /cadastre o primeiro para poder lançar despesas/,
  itens: [
    fabricarTipoDespesa({ id: 1, nome: 'Pedágio', ativo: true }),
    fabricarTipoDespesa({ id: 2, nome: 'Lavagem', ativo: false }),
  ],
  descricao: (t) => `o tipo ${t.nome}`,
  rotuloNome: 'Nome',
  rotuloSituacao: 'Situação',
  situacao: { ativo: 'Ativo', inativo: 'Inativo' },
  alternar: { desativar: 'Inativar', ativar: 'Ativar' },
  preencherNovo: async (user, dialogo) => {
    await user.type(within(dialogo).getByLabelText('Nome'), 'Multa de trânsito')
  },
  corpoNovo: { nome: 'Multa de trânsito' },
  corpoRenomeado: () => ({ nome: 'Renomeado', ativo: true }),
  corpoInativado: (t) => ({ nome: t.nome, ativo: false }),
})
