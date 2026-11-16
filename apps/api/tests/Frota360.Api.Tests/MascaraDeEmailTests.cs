using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Frota360.Api.Tests
{
    public class MascaraDeEmailTests
    {
        private sealed class Coletor : ILogEventSink
        {
            public List<LogEvent> Eventos { get; } = [];
            public void Emit(LogEvent logEvent) => Eventos.Add(logEvent);
        }

        private static LogEvent Registrar(Action<ILogger> escrever)
        {
            var coletor = new Coletor();
            using (var logger = new LoggerConfiguration().Enrich.With<MascaraDeEmail>().WriteTo.Sink(coletor).CreateLogger())
                escrever(logger);
            return coletor.Eventos.Single();
        }

        [Theory]
        [InlineData("paulo@gmail.com", "p***@gmail.com")]
        [InlineData("Maria.Silva+frota@empresa.com.br", "M***@empresa.com.br")]
        [InlineData("a@x.io", "a***@x.io")]
        [InlineData("contato: joao@frota360app.com.br, obrigado", "contato: j***@frota360app.com.br, obrigado")]
        [InlineData("dois: a1@x.com e b2@y.com", "dois: a***@x.com e b***@y.com")]
        public void Mascarar_DeveEsconderAParteLocalEManterODominio(string texto, string esperado)
        {
            Assert.Equal(esperado, MascaraDeEmail.Mascarar(texto));
        }

        [Theory]
        [InlineData("ABC1D23")]
        [InlineData("Frota360")]
        [InlineData("@usuario sem domínio")]
        [InlineData("https://app.frota360app.com.br/convite?token=abc")]
        public void Mascarar_SemEmail_DeveDevolverOTextoIntacto(string texto)
        {
            Assert.Equal(texto, MascaraDeEmail.Mascarar(texto));
        }

        [Fact]
        public void Enriquecedor_DeveMascararAPropriedadeEAMensagemRenderizada()
        {
            var evento = Registrar(log => log.Information("Tentativa de login para o email {Email}", "paulo@gmail.com"));

            Assert.Equal("p***@gmail.com", ((ScalarValue)evento.Properties["Email"]).Value);
            Assert.Equal("Tentativa de login para o email \"p***@gmail.com\"", evento.RenderMessage());
        }

        [Fact]
        public void Enriquecedor_DeveMascararQualquerPropriedadeEPreservarAsDemais()
        {
            var evento = Registrar(log => log.Information("E-mail enviado para {Para} | Empresa {EmpresaId} | {Placa}",
                "joao@empresa.com", 7, "ABC1D23"));

            Assert.Equal("j***@empresa.com", ((ScalarValue)evento.Properties["Para"]).Value);
            Assert.Equal(7, ((ScalarValue)evento.Properties["EmpresaId"]).Value);
            Assert.Equal("ABC1D23", ((ScalarValue)evento.Properties["Placa"]).Value);
        }
    }
}
