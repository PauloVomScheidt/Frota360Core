using Frota360.Application.DTOs.TipoManutencao.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.TiposManutencao
{
    /// <summary>Mapeamento centralizado de <see cref="TipoManutencao"/> para <see cref="TipoManutencaoResponse"/>.</summary>
    public static class TipoManutencaoMappings
    {
        public static TipoManutencaoResponse ToResponse(this TipoManutencao t) => new()
        {
            Id = t.Id,
            Nome = t.Nome,
            IntervaloKm = t.IntervaloKm,
            Ativo = t.Ativo,
            DataInclusao = t.DataInclusao
        };
    }
}
