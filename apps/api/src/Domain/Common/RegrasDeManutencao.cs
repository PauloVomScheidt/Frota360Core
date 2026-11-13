namespace Frota360.Domain.Common
{
    public static class RegrasDeManutencao
    {
        /// <summary>
        /// Quantos km antes do previsto uma pendência passa a contar como "vencendo".
        /// Espelhada em <c>FAIXA_AVISO</c> (<c>apps/web/src/lib/manutencao.ts</c>) — mexeu numa, mexa na outra.
        /// </summary>
        public const int FaixaAvisoKm = 500;

        /// <summary>Teto de itens em <c>ResumoManutencoesResponse.Urgentes</c>; as contagens não são limitadas.</summary>
        public const int LimiteUrgentes = 10;
    }
}
