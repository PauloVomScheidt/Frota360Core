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
    }
}
