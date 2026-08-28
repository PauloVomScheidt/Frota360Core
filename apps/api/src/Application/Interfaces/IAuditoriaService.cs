using Frota360.Application.Common;
using Frota360.Domain.Entities;

namespace Frota360.Application.Interfaces
{
    /// <summary>
    /// Escrita da trilha de auditoria. A leitura é uma query CQRS (<c>GetLogsAuditoriaQuery</c>),
    /// não passa por aqui.
    ///
    /// Nenhum método propaga exceção: auditoria não derruba a operação de negócio que a
    /// originou. Falha vira log de erro no Serilog e a requisição segue.
    /// </summary>
    public interface IAuditoriaService
    {
        /// <summary>
        /// Registra uma ação do usuário autenticado. Chame ao final do caminho feliz, depois
        /// de a persistência ter sucedido.
        /// </summary>
        /// <param name="entidade">Uma constante de <c>EntidadesAuditadas</c>.</param>
        /// <param name="acao">Uma constante de <c>AcoesAuditoria</c>.</param>
        /// <param name="descricao">Frase pronta em português, exibida na listagem.</param>
        /// <param name="alteracoes">Diff de <c>AlteracoesBuilder</c>; nulo em criação e exclusão.</param>
        Task RegistrarAsync(string entidade, string acao, int? entidadeId, string descricao,
                            IEnumerable<AlteracaoCampo>? alteracoes = null);

        /// <summary>
        /// Versão para fluxo sem sessão, com o ator passado à mão. Existe por causa do aceite
        /// de convite: é anônimo, e o usuário que age nasce na própria operação.
        /// </summary>
        Task RegistrarComoAsync(int empresaId, Usuario ator, string entidade, string acao,
                                int? entidadeId, string descricao,
                                IEnumerable<AlteracaoCampo>? alteracoes = null);
    }
}
