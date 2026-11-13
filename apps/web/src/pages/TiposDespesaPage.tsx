import { tiposDespesaApi } from '../api/tiposDespesa'
import type { TipoDespesaResponse } from '../api/types'
import { pode } from '../auth/permissions'
import { CatalogoSimples } from '../components/CatalogoSimples'

export function TiposDespesaPage() {
  return (
    <CatalogoSimples<TipoDespesaResponse>
      api={tiposDespesaApi}
      chave="tiposDespesa"
      // O nome do tipo é desnormalizado na despesa e é a `categoria` da linha de custo.
      invalidarTambem={['despesas', 'custos']}
      podeEditar={pode.editarTiposDespesa}
      idCampos="TipoDespesa"
      titulo="Tipos de despesa"
      subtitulo="Catálogo da empresa — alimenta o seletor da tela de despesas."
      tituloNovo="Novo tipo de despesa"
      tituloEdicao="Editar tipo de despesa"
      placeholder="Ex.: Pedágio"
      nota="O nome é único por empresa e aparece como categoria na tela de custos. Tipo inativo some do seletor de lançamento, mas continua nomeando as despesas antigas."
      textoErro="Não foi possível carregar os tipos de despesa."
      textoVazio="Nenhum tipo cadastrado ainda — cadastre o primeiro para poder lançar despesas."
      tituloExclusao="Excluir tipo de despesa"
      mensagemExclusao={(nome) =>
        `"${nome}" será removido do catálogo. Se já houver despesas usando este tipo, a API recusa a exclusão — nesse caso, inative-o para tirá-lo do seletor sem apagar o histórico.`
      }
    />
  )
}
