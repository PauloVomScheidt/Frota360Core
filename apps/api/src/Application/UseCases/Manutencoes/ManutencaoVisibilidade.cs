using Frota360.Application.Common;
using Frota360.Application.DTOs.Manutencao.Response;
using Frota360.Application.Interfaces;

namespace Frota360.Application.UseCases.Manutencoes
{
    public static class ManutencaoVisibilidade
    {
        /// <summary>
        /// O motorista enxerga a manutenção para saber o estado do veículo que vai pegar —
        /// não para saber quanto a empresa gasta. O custo é o único campo comercialmente
        /// sensível da resposta, então some para essa role.
        /// </summary>
        public static ManutencaoResponse SemCustoParaMotorista(this ManutencaoResponse resposta, ICurrentUserService currentUser)
        {
            if (currentUser.EhMotorista())
                resposta.Custo = null;

            return resposta;
        }
    }
}
