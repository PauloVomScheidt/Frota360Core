using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Abastecimento.Response;

namespace Frota360.Application.UseCases.Abastecimentos.Queries.GetAbastecimentoAnterior
{
    /// <summary>
    /// <paramref name="IgnorarId"/> tira o próprio registro da conta quando a tela está
    /// corrigindo um lançamento existente — senão ele seria a própria referência.
    /// Devolve <c>null</c> no primeiro abastecimento do veículo, onde não há de onde partir.
    /// </summary>
    public sealed record GetAbastecimentoAnteriorQuery(int VeiculoId, int Odometro, int? IgnorarId = null)
        : IQuery<AbastecimentoAnteriorResponse?>;
}
