using Frota360.Application.DTOs.TipoCombustivel.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.TiposCombustivel
{
    /// <summary>Mapeamento centralizado de <see cref="TipoCombustivel"/> para <see cref="TipoCombustivelResponse"/>.</summary>
    public static class TipoCombustivelMappings
    {
        public static TipoCombustivelResponse ToResponse(this TipoCombustivel t) => new()
        {
            Id = t.Id,
            Nome = t.Nome,
            Ativo = t.Ativo,
            DataInclusao = t.DataInclusao
        };
    }
}
