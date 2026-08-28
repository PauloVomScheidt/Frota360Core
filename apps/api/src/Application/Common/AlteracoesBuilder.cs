using System.Globalization;

namespace Frota360.Application.Common
{
    /// <summary>
    /// Monta o diff de uma edição, campo a campo. Chame <b>antes</b> de aplicar o request na
    /// entidade — ela é mutada in-place pelos handlers, então depois disso o "antes" já se perdeu.
    ///
    /// <code>
    /// var alteracoes = new AlteracoesBuilder()
    ///     .Comparar("Placa", veiculo.Placa, request.Placa)
    ///     .Comparar("Quilometragem", veiculo.Quilometragem, request.Quilometragem)
    ///     .Construir();
    /// </code>
    ///
    /// É explícito de propósito: quem escreve o handler escolhe o que entra no histórico, e
    /// nenhum hash de senha ou token vaza por reflexão sobre a entidade inteira.
    /// </summary>
    public sealed class AlteracoesBuilder
    {
        private readonly List<AlteracaoCampo> _alteracoes = [];

        /// <summary>Registra o campo apenas quando os dois valores diferem.</summary>
        public AlteracoesBuilder Comparar<T>(string campo, T? de, T? para)
        {
            if (EqualityComparer<T?>.Default.Equals(de, para))
                return this;

            _alteracoes.Add(new AlteracaoCampo(campo, Formatar(de), Formatar(para)));
            return this;
        }

        /// <summary>Null quando nada mudou — a coluna <c>Alteracoes</c> fica vazia em vez de "[]".</summary>
        public IReadOnlyList<AlteracaoCampo>? Construir()
            => _alteracoes.Count == 0 ? null : _alteracoes;

        /// <summary>
        /// Cultura invariante para data e número: o valor gravado é imutável e não deve
        /// depender da cultura do processo que o escreveu. A tela formata na leitura.
        /// </summary>
        private static string? Formatar<T>(T? valor) => valor switch
        {
            null => null,
            bool b => b ? "Sim" : "Não",
            DateTime d => d.ToString("o", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => valor.ToString()
        };
    }
}
