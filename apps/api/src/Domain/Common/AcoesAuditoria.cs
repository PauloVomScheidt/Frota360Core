namespace Frota360.Domain.Common
{
    /// <summary>
    /// Verbos da trilha de auditoria. Combinados com <see cref="EntidadesAuditadas"/> dão a
    /// semântica do evento ("Rota" + "Encerrou") — dois eixos de filtro com poucos valores
    /// distintos, em vez de uma constante por evento ("RotaEncerrada", "VeiculoCriado"...).
    /// </summary>
    public static class AcoesAuditoria
    {
        public const string Criou = "Criou";
        public const string Atualizou = "Atualizou";
        public const string Excluiu = "Excluiu";

        /// <summary>Transições de estado, que não passam por PUT: encerrar rota, concluir manutenção.</summary>
        public const string Encerrou = "Encerrou";
        public const string Concluiu = "Concluiu";

        public const string AlterouPermissao = "AlterouPermissao";
        public const string Ativou = "Ativou";
        public const string Desativou = "Desativou";

        public const string Cancelou = "Cancelou";
        public const string Aceitou = "Aceitou";

        /// <summary>Vocabulário fechado — o validator do filtro recusa o que estiver fora daqui.</summary>
        public static readonly IReadOnlyList<string> Todas =
        [
            Criou, Atualizou, Excluiu, Encerrou, Concluiu,
            AlterouPermissao, Ativou, Desativou, Cancelou, Aceitou
        ];
    }
}
