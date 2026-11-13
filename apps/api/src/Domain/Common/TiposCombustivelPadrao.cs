namespace Frota360.Domain.Common
{
    /// <summary>
    /// Catálogo inicial semeado quando uma empresa é provisionada, para que a tela de
    /// abastecimento já abra utilizável. A empresa edita, inativa ou acrescenta o que
    /// quiser depois — daqui em diante o catálogo é dela.
    ///
    /// ⚠️ Esta lista está <b>duplicada</b> no <c>Up</c> da migration
    /// <c>AbastecimentoDetalhadoECatalogos</c>, que semeou as empresas já existentes na
    /// virada. A migration é histórica e não deve ser reescrita: acrescentar um item aqui
    /// vale só para empresa nova.
    ///
    /// Não há equivalente para <see cref="Entities.Posto"/> — rede credenciada não tem
    /// padrão, cada empresa cadastra a sua.
    ///
    /// ⚠️ <b>Só combustível entra nesta lista.</b> ARLA 32 já esteve aqui e saiu: é reagente,
    /// não combustível, e um lançamento dele com odômetro parte o trecho em dois e estraga o
    /// km/l do abastecimento seguinte. O catálogo não tem como distinguir os dois casos — a
    /// empresa que cadastrar um não-combustível à mão volta a ter o problema.
    /// </summary>
    public static class TiposCombustivelPadrao
    {
        public static readonly IReadOnlyList<string> Itens =
        [
            "Diesel S10",
            "Diesel S500",
            "Gasolina comum",
            "Gasolina aditivada",
            "Etanol",
            "GNV"
        ];
    }
}
