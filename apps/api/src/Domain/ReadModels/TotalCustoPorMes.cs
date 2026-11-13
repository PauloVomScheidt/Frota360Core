using Frota360.Domain.Enums;

namespace Frota360.Domain.ReadModels
{
    /// <summary>Soma de um mês em <b>uma</b> origem — o handler pivota as duas numa linha só.</summary>
    public sealed record TotalCustoPorMes(
        int Ano,
        int Mes,
        OrigemCusto Origem,
        decimal Total);
}
