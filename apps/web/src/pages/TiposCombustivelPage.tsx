import { tiposCombustivelApi } from '../api/tiposCombustivel'
import type { TipoCombustivelResponse } from '../api/types'
import { pode } from '../auth/permissions'
import { CatalogoSimples } from '../components/CatalogoSimples'

export function TiposCombustivelPage() {
  return (
    <CatalogoSimples<TipoCombustivelResponse>
      api={tiposCombustivelApi}
      chave="tiposCombustivel"
      // O nome do combustível é desnormalizado na listagem de abastecimentos. `['custos']` fica de
      // fora de propósito: a categoria do abastecimento é a constante "Combustível", não o nome do tipo.
      invalidarTambem={['abastecimentos']}
      podeEditar={pode.editarTiposCombustivel}
      idCampos="TipoCombustivel"
      titulo="Tipos de combustível"
      subtitulo="Catálogo da empresa — alimenta o seletor da tela de abastecimentos."
      tituloNovo="Novo tipo de combustível"
      tituloEdicao="Editar tipo de combustível"
      placeholder="Ex.: Diesel S10"
      nota="O nome é único por empresa. Combustível inativo some do seletor de lançamento, mas continua nomeando os abastecimentos antigos."
      textoErro="Não foi possível carregar os tipos de combustível."
      textoVazio="Nenhum tipo cadastrado ainda — cadastre o primeiro para poder lançar abastecimentos."
      tituloExclusao="Excluir tipo de combustível"
      mensagemExclusao={(nome) =>
        `"${nome}" será removido do catálogo. Se já houver abastecimentos usando este combustível, a API recusa a exclusão — nesse caso, inative-o para tirá-lo do seletor sem apagar o histórico.`
      }
    />
  )
}
