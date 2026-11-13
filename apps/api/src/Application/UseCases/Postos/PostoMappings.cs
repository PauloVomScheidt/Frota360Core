using Frota360.Application.DTOs.Posto.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Postos
{
    /// <summary>Mapeamento centralizado de <see cref="Posto"/> para <see cref="PostoResponse"/>.</summary>
    public static class PostoMappings
    {
        public static PostoResponse ToResponse(this Posto p) => new()
        {
            Id = p.Id,
            Nome = p.Nome,
            Cnpj = p.Cnpj,
            Cidade = p.Cidade,
            Ativo = p.Ativo,
            DataInclusao = p.DataInclusao
        };
    }
}
