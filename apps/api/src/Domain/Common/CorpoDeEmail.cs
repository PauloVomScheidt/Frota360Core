namespace Frota360.Domain.Common
{
    /// <summary>
    /// As duas versões do mesmo e-mail. Mensagem só em HTML é sinal de spam para a maioria
    /// dos filtros — manter as duas juntas num tipo evita que uma seja editada sem a outra.
    /// </summary>
    public sealed record CorpoDeEmail(string Html, string Texto)
    {
        /// <summary>
        /// Formato dos e-mails transacionais do sistema: uma frase, uma ação com link e um
        /// aviso de "ignore se não foi você". A URL aparece também em texto porque cliente
        /// de e-mail que bloqueia link deixaria a mensagem sem saída.
        /// </summary>
        public static CorpoDeEmail ComLink(string chamada, string acao, string link, string aviso) => new(
            Html: $"""
                <p>{chamada}</p>
                <p><a href="{link}">{acao}</a></p>
                <p>Se o link não abrir, copie e cole este endereço no navegador:<br>{link}</p>
                <p>{aviso}</p>
                """,
            Texto: $"""
                {chamada}

                {acao}:
                {link}

                {aviso}
                """);
    }
}
