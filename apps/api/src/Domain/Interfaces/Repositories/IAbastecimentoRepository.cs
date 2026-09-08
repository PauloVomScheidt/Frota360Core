using Frota360.Domain.Common;
using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IAbastecimentoRepository
    {
        /// <summary>
        /// Uma página da lista da empresa, do mais recente para o mais antigo, com veículo, rota,
        /// motorista, usuário, combustível e posto carregados. Devolve também o total que
        /// satisfaz o filtro, ignorando a paginação.
        ///
        /// ⚠️ A ordenação precisa ser <b>total</b> (data + id): sem o desempate, a página 2 pode
        /// repetir ou pular linhas que a 1 já mostrou.
        /// </summary>
        Task<(IEnumerable<Abastecimento> Itens, int Total)> ConsultarAsync(int empresaId, FiltroAbastecimento filtro);

        /// <summary>
        /// Contagem e soma do <b>filtro inteiro</b>, para o rodapé da tela. Ignora
        /// <c>Pagina</c>/<c>TamanhoPagina</c> do filtro de propósito — é o número do recorte,
        /// não o da página.
        /// </summary>
        Task<ResumoLancamentos> ResumirAsync(int empresaId, FiltroAbastecimento filtro);

        /// <summary>
        /// O abastecimento de <b>maior odômetro abaixo</b> de <paramref name="odometro"/> naquele
        /// veículo — a referência da estimativa de km/l, no método tanque a tanque.
        ///
        /// Ordenar por odômetro, e não por data, é o que impede um lançamento retroativo de virar
        /// quilometragem negativa. <paramref name="ignorarId"/> tira o próprio registro da conta
        /// quando a tela está corrigindo um lançamento existente.
        ///
        /// ⚠️ Enxerga o histórico <b>do veículo</b>, sem recorte por motorista: o consumo é
        /// propriedade do caminhão, não de quem dirigiu. Ver a nota em <c>GetAnterior</c> no
        /// controller sobre o que a resposta pode expor.
        /// </summary>
        Task<Abastecimento?> GetAnteriorPorOdometroAsync(int empresaId, int veiculoId, int odometro, int? ignorarId = null);

        Task<Abastecimento?> GetByIdAsync(int id, int empresaId);

        /// <summary>
        /// Usado antes de excluir um veículo, que não pode sumir deixando abastecimentos
        /// apontando para um registro que não existe mais (mesma regra da rota, RN08).
        /// </summary>
        Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId);

        /// <summary>
        /// Usado antes de excluir um item do catálogo de combustíveis: tipo em uso não some,
        /// é inativado — apagá-lo levaria junto o histórico de abastecimento.
        /// </summary>
        Task<bool> ExisteComTipoCombustivelAsync(int empresaId, int tipoCombustivelId);

        /// <summary>Mesma regra do combustível, para o posto credenciado.</summary>
        Task<bool> ExisteComPostoAsync(int empresaId, int postoId);

        Task<Abastecimento> AddAsync(Abastecimento abastecimento);
        Task<Abastecimento> UpdateAsync(Abastecimento abastecimento);
        Task DeleteAsync(Abastecimento abastecimento);
    }
}
