namespace Frota360.Domain.Common
{
    /// <summary>
    /// Catálogo inicial semeado quando uma empresa é provisionada, para que a tela de
    /// manutenção já abra utilizável. A empresa edita, inativa ou acrescenta o que quiser
    /// depois — daqui em diante o catálogo é dela.
    /// </summary>
    public static class TiposManutencaoPadrao
    {
        /// <summary>Nome do tipo e o intervalo em km usual, quando existe um.</summary>
        public static readonly IReadOnlyList<(string Nome, int? IntervaloKm)> Itens =
        [
            ("Troca de óleo", 10_000),
            ("Troca de filtro de óleo", 10_000),
            ("Troca de filtro de ar", 20_000),
            ("Rodízio de pneus", 10_000),
            ("Troca de pneus", 40_000),
            ("Alinhamento e balanceamento", 10_000),
            ("Troca de pastilhas de freio", 30_000),
            ("Revisão geral", 20_000),
            ("Troca de correia dentada", 60_000),
            ("Troca de bateria", null)
        ];
    }
}
