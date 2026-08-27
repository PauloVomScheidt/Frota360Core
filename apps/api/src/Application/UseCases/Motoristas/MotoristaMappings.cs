using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Motoristas
{
    /// <summary>Mapeamento centralizado de <see cref="Motorista"/> para <see cref="MotoristaResponse"/>.</summary>
    public static class MotoristaMappings
    {
        public static MotoristaResponse ToResponse(this Motorista m) => new()
        {
            Id = m.Id,
            Nome = m.Nome,
            Email = m.Email,
            CPF = m.CPF,
            DataNascimento = m.DataNascimento,
            DataInclusao = m.DataInclusao
        };
    }
}
