namespace Frota360.Application.Interfaces
{
    /// <summary>
    /// Dados do usuário autenticado na requisição atual, extraídos das claims do JWT.
    /// Handlers usam esta abstração para escopar toda operação à empresa do usuário —
    /// o EmpresaId nunca deve vir do corpo/query da requisição.
    /// </summary>
    public interface ICurrentUserService
    {
        int UsuarioId { get; }
        int EmpresaId { get; }
        string Role { get; }

        /// <summary>
        /// Nome e e-mail vêm das claims <c>name</c>/<c>email</c>, que o token já emite —
        /// a trilha de auditoria os desnormaliza em cada linha sem uma ida a mais ao banco.
        /// </summary>
        string Nome { get; }
        string Email { get; }

        /// <summary>IP de origem da requisição, quando disponível. Só a auditoria consome.</summary>
        string? IpOrigem { get; }
    }
}
