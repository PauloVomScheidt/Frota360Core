import { within } from '@testing-library/react'
import { fabricarTipoCombustivel } from '../test/fabricas'
import { testarCatalogo } from '../test/suiteCatalogo'
import { TiposCombustivelPage } from './TiposCombustivelPage'

testarCatalogo({
  tela: () => <TiposCombustivelPage />,
  titulo: 'Tipos de combustível',
  recurso: '/tipocombustivel',
  botaoNovo: 'Novo tipo',
  dialogoNovo: 'Novo tipo de combustível',
  dialogoEdicao: 'Editar tipo de combustível',
  tituloExclusao: 'Excluir tipo de combustível',
  textoVazio: /cadastre o primeiro para poder lançar abastecimentos/,
  itens: [
    fabricarTipoCombustivel({ id: 1, nome: 'Diesel S10', ativo: true }),
    fabricarTipoCombustivel({ id: 2, nome: 'Gasolina comum', ativo: false }),
  ],
  descricao: (t) => `o tipo ${t.nome}`,
  rotuloNome: 'Nome',
  rotuloSituacao: 'Situação',
  situacao: { ativo: 'Ativo', inativo: 'Inativo' },
  alternar: { desativar: 'Inativar', ativar: 'Ativar' },
  preencherNovo: async (user, dialogo) => {
    await user.type(within(dialogo).getByLabelText('Nome'), ' GNV ')
  },
  corpoNovo: { nome: 'GNV' },
  corpoRenomeado: () => ({ nome: 'Renomeado', ativo: true }),
  corpoInativado: (t) => ({ nome: t.nome, ativo: false }),
})
