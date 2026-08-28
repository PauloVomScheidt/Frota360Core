namespace Frota360.Domain.Common
{
    /// <summary>
    /// Roles do sistema (matriz de permissões no PLANO-AUTH-ROLES.md):
    /// Admin gerencia tudo (único que exclui e administra usuários);
    /// Supervisor cria/edita motoristas, veículos e rotas;
    /// Operador cria/edita apenas rotas e visualiza o restante;
    /// Motorista enxerga apenas as próprias rotas — abre e encerra, nada mais.
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Supervisor = "Supervisor";
        public const string Operador = "Operador";

        /// <summary>
        /// Diferente das outras, esta role exige vínculo: o usuário precisa de um
        /// <c>MotoristaId</c>, que é o segundo eixo de isolamento (além do EmpresaId).
        /// </summary>
        public const string Motorista = "Motorista";

        /// <summary>Quem opera as telas de gestão da frota — todos menos o motorista.</summary>
        public const string Gestao = $"{Admin},{Supervisor},{Operador}";
    }
}
