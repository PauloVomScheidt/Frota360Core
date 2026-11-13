namespace Frota360.Domain.Common
{
    /// <summary>
    /// Catálogo inicial semeado quando uma empresa é provisionada, para que a tela de
    /// despesas já abra utilizável. A empresa edita, inativa ou acrescenta o que quiser
    /// depois — daqui em diante o catálogo é dela.
    ///
    /// ⚠️ Esta lista está <b>duplicada</b> no <c>Up</c> da migration <c>DespesasAvulsas</c>,
    /// que semeou as empresas já existentes na virada. A migration é histórica e não deve
    /// ser reescrita: acrescentar um item aqui vale só para empresa nova.
    /// </summary>
    public static class TiposDespesaPadrao
    {
        public static readonly IReadOnlyList<string> Itens =
        [
            "Pedágio",
            "Multa de trânsito",
            "IPVA",
            "Licenciamento",
            "Seguro",
            "Lavagem",
            "Estacionamento"
        ];
    }
}
