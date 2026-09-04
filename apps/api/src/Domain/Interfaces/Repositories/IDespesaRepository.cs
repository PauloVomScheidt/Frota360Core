using Frota360.Domain.Common;
using Frota360.Domain.Entities;

namespace Frota360.Domain.Interfaces.Repositories
{
    public interface IDespesaRepository
    {
        /// <summary>
        /// Uma página da lista da empresa, da mais recente para a mais antiga, com veículo, tipo e
        /// motorista carregados. Devolve também o total que satisfaz o filtro, ignorando a
        /// paginação.
        ///
        /// ⚠️ A ordenação precisa ser <b>total</b> (data + id): sem o desempate, a página 2 pode
        /// repetir ou pular linhas que a 1 já mostrou.
        /// </summary>
        Task<(IEnumerable<Despesa> Itens, int Total)> ConsultarAsync(int empresaId, FiltroDespesa filtro);

        /// <summary>
        /// Contagem e soma do <b>filtro inteiro</b>, para o rodapé da tela. Ignora
        /// <c>Pagina</c>/<c>TamanhoPagina</c> de propósito — é o número do recorte, não o da página.
        /// </summary>
        Task<ResumoLancamentos> ResumirAsync(int empresaId, FiltroDespesa filtro);

        Task<Despesa?> GetByIdAsync(int id, int empresaId);

        /// <summary>
        /// Usado antes de excluir um veículo, que não pode sumir deixando despesas
        /// apontando para um registro que não existe mais (RN08, terceira guarda).
        /// </summary>
        Task<bool> ExisteComVeiculoAsync(int empresaId, int veiculoId);

        /// <summary>Usado antes de excluir um tipo do catálogo: em uso, ele só pode ser inativado.</summary>
        Task<bool> ExisteComTipoAsync(int empresaId, int tipoDespesaId);

        Task<Despesa> AddAsync(Despesa despesa);
        Task<Despesa> UpdateAsync(Despesa despesa);
        Task DeleteAsync(Despesa despesa);
    }
}
