using Frota360.Application.DTOs.Custo.Request;
using Frota360.Application.DTOs.Custo.Response;
using Frota360.Domain.Common;
using Frota360.Domain.Enums;
using Frota360.Domain.ReadModels;

namespace Frota360.Application.UseCases.Custos
{
    public static class CustoMappings
    {
        public static LancamentoCustoResponse ToResponse(this LancamentoCusto lancamento) => new()
        {
            Origem = lancamento.Origem.ToString(),
            OrigemId = lancamento.OrigemId,
            Data = lancamento.Data,
            VeiculoId = lancamento.VeiculoId,
            VeiculoNome = lancamento.VeiculoNome,
            VeiculoPlaca = lancamento.VeiculoPlaca,
            MotoristaId = lancamento.MotoristaId,
            MotoristaNome = lancamento.MotoristaNome,
            Categoria = lancamento.Categoria,
            Valor = lancamento.Valor,
            Observacao = lancamento.Observacao
        };

        public static FiltroCusto ParaFiltro(this ConsultarCustosRequest request)
            => new(request.VeiculoId, request.MotoristaId, OrigemDe(request.Origem), request.De, request.Ate);

        public static FiltroCusto ParaFiltro(this ResumoCustosRequest request)
            => new(request.VeiculoId, request.MotoristaId, OrigemDe(request.Origem), request.De, request.Ate);

        /// <summary>
        /// O validator já barrou o texto inválido, então aqui um valor que não converte vira
        /// "sem filtro de origem" em vez de exceção.
        /// </summary>
        private static OrigemCusto? OrigemDe(string? origem)
            => Enum.TryParse<OrigemCusto>(origem, ignoreCase: true, out var valor) ? valor : null;
    }
}
