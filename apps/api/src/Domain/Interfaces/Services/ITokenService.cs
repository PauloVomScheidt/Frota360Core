using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Services
{
    public interface ITokenService
    {
        string GerarToken(Usuario usuario);
        string GerarRefreshToken();
    }
}
