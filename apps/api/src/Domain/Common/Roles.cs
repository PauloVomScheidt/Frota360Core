namespace Frota360.Domain.Common
{
    /// <summary>
    /// Roles do sistema (matriz de permissões no PLANO-AUTH-ROLES.md):
    /// Admin gerencia tudo (único que exclui e administra usuários);
    /// Supervisor cria/edita motoristas, veículos e rotas;
    /// Operador cria/edita apenas rotas e visualiza o restante.
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Supervisor = "Supervisor";
        public const string Operador = "Operador";
    }
}
