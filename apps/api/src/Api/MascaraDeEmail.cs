using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Frota360.Api
{
    /// <summary>
    /// Troca todo e-mail que aparecer nas propriedades de um log por "p***@dominio" antes de qualquer
    /// destino — console e Grafana Cloud. Os logs saem da AWS, e o e-mail de quem tenta logar não
    /// é necessário para diagnosticar: o domínio e o id do usuário bastam (LGPD, minimização).
    /// Atua pelo valor, não pelo nome da propriedade, para cobrir também os logs que ainda não existem.
    /// </summary>
    public sealed partial class MascaraDeEmail : ILogEventEnricher
    {
        [GeneratedRegex(@"([A-Za-z0-9])[A-Za-z0-9._%+-]*@([A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,})")]
        private static partial Regex Email();

        public static string Mascarar(string texto) =>
            texto.Contains('@') ? Email().Replace(texto, "$1***@$2") : texto;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var comEmail = logEvent.Properties
                .Where(p => p.Value is ScalarValue { Value: string texto } && texto.Contains('@'))
                .ToList();

            foreach (var (nome, valor) in comEmail)
                logEvent.AddOrUpdateProperty(new LogEventProperty(nome, new ScalarValue(Mascarar((string)((ScalarValue)valor).Value!))));
        }
    }
}
