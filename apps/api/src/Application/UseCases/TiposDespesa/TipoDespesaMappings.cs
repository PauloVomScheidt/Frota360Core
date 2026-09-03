using Frota360.Application.DTOs.TipoDespesa.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.TiposDespesa
{
    /// <summary>Mapeamento centralizado de <see cref="TipoDespesa"/> para <see cref="TipoDespesaResponse"/>.</summary>
    public static class TipoDespesaMappings
    {
        public static TipoDespesaResponse ToResponse(this TipoDespesa t) => new()
        {
            Id = t.Id,
            Nome = t.Nome,
            Ativo = t.Ativo,
            DataInclusao = t.DataInclusao
        };
    }
}
