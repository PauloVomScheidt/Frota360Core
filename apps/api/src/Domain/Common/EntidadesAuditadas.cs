namespace Frota360.Domain.Common
{
    /// <summary>
    /// O que a trilha de auditoria acompanha. Não inclui <c>Empresa</c> (provisionamento não
    /// tem ator logado) nem sessão (login/logout ficam no Serilog: muito volume, pouco valor).
    /// </summary>
    public static class EntidadesAuditadas
    {
        public const string Veiculo = "Veiculo";
        public const string Rota = "Rota";
        public const string Manutencao = "Manutencao";
        public const string Abastecimento = "Abastecimento";
        public const string TipoManutencao = "TipoManutencao";
        public const string Despesa = "Despesa";
        public const string TipoDespesa = "TipoDespesa";
        public const string Usuario = "Usuario";
        public const string Convite = "Convite";

        /// <summary>Vocabulário fechado — o validator do filtro recusa o que estiver fora daqui.</summary>
        public static readonly IReadOnlyList<string> Todas =
        [
            Veiculo, Rota, Manutencao, Abastecimento, TipoManutencao,
            Despesa, TipoDespesa, Usuario, Convite
        ];
    }
}
