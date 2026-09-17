using Frota360.Domain.Common;

namespace Frota360.Domain.Interfaces.Services
{
    /// <summary>
    /// Cálculo de trajeto entre dois pontos pela Google Routes API.
    /// A implementação <b>não lança</b>: rede, timeout e resposta inesperada viram
    /// <see cref="ResultadoCalculoRota.Falha"/>.
    /// </summary>
    public interface IGoogleRoutesClient
    {
        Task<ResultadoCalculoRota> CalcularAsync(Coordenada origem, Coordenada destino,
                                                 CancellationToken cancellationToken = default);
    }
}
