namespace Frota360.Domain.Common
{
    /// <summary>
    /// Forma canônica do e-mail: sem espaços nas pontas e em minúsculas.
    ///
    /// Existe porque, no SQL Server, a collation padrão do banco era case-insensitive e
    /// fazia <c>Email == email</c> casar "Fulano@Empresa.com" com "fulano@empresa.com"
    /// por acidente — o índice único de <c>Usuario.Email</c> também barrava duplicata de
    /// caixa sem que nada no código pedisse isso. O PostgreSQL compara texto de forma
    /// case-sensitive, então essa garantia deixaria de existir em silêncio: quem digitasse
    /// o e-mail com outra caixa não conseguiria entrar, e dois cadastros "iguais" passariam.
    ///
    /// A normalização mora no código, e não numa collation do banco ou no tipo
    /// <c>citext</c>, para a regra ficar explícita, testável e independente de fornecedor —
    /// o mesmo motivo que levou à troca de banco.
    ///
    /// Vale só para e-mail. Os hashes de token (<c>TokenHash</c>, <c>RefreshTokenHash</c>,
    /// <c>ResetSenhaTokenHash</c>) são Base64 e case-sensitive de verdade: comparação
    /// exata é o comportamento correto para eles.
    /// </summary>
    public static class EmailNormalizado
    {
        public static string De(string email) => email.Trim().ToLowerInvariant();
    }
}
