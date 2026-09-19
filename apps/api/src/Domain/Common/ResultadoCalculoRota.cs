namespace Frota360.Domain.Common
{
    /// <summary>
    /// Devolução do cálculo de trajeto. É um resultado, não uma exceção: a Routes API é um
    /// terceiro que sai do ar, atrasa e muda de formato, e nenhuma dessas três coisas deve
    /// chegar ao usuário como erro 500 — quem chama decide o que fazer com a falha.
    /// </summary>
    public sealed record ResultadoCalculoRota
    {
        private ResultadoCalculoRota() { }

        public bool Sucesso { get; private init; }

        public int DistanciaMetros { get; private init; }
        public int DuracaoEstimadaSegundos { get; private init; }
        public string? PolylineCodificada { get; private init; }

        /// <summary>
        /// Por que falhou, em linguagem de diagnóstico. Vai para o log; não é texto de tela.
        /// </summary>
        public string? Motivo { get; private init; }

        public static ResultadoCalculoRota Ok(int distanciaMetros, int duracaoSegundos, string? polyline) => new()
        {
            Sucesso = true,
            DistanciaMetros = distanciaMetros,
            DuracaoEstimadaSegundos = duracaoSegundos,
            PolylineCodificada = polyline
        };

        public static ResultadoCalculoRota Falha(string motivo) => new()
        {
            Sucesso = false,
            Motivo = motivo
        };
    }
}
