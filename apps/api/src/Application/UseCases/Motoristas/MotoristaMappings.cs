using Frota360.Application.DTOs.Motorista.Response;
using Frota360.Domain.Entities;

namespace Frota360.Application.UseCases.Motoristas
{
    /// <summary>Mapeamento de <see cref="Usuario"/> (role Motorista) para <see cref="MotoristaResponse"/>.</summary>
    public static class MotoristaMappings
    {
        public static MotoristaResponse ToMotoristaResponse(this Usuario u) => new()
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            CPF = u.CPF,
            DataNascimento = u.DataNascimento,
            Ativo = u.Ativo,
            DataInclusao = u.DataInclusao
        };
    }
}
