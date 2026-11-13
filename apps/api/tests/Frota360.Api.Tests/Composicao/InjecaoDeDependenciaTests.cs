using System.Text.Json;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.UseCases.Veiculos.Queries.GetVeiculoById;
using Frota360.Domain.Common;
using Frota360.Domain.Interfaces.Services;
using Frota360.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Frota360.Api.Tests.Composicao
{
    public class InjecaoDeDependenciaTests
    {
        [Fact]
        public void Montagem_DeveResolverTodoServicoDoFrota360()
        {
            var servicos = Composicao.Servicos();
            var tipos = servicos
                .Where(d => d.ServiceType.FullName?.StartsWith("Frota360.") == true)
                .Select(d => d.ServiceType)
                .Distinct()
                .ToList();

            using var provedor = servicos.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            using var escopo = provedor.CreateScope();

            Assert.NotEmpty(tipos);
            Assert.All(tipos, tipo => Assert.NotNull(escopo.ServiceProvider.GetRequiredService(tipo)));
        }

        [Fact]
        public void AddApplication_DeveRegistrarHandlersPeloContratoFechado()
        {
            using var provedor = Composicao.Provedor();
            using var escopo = provedor.CreateScope();

            var handler = escopo.ServiceProvider.GetRequiredService<IRequestHandler<GetVeiculoByIdQuery, VeiculoResponse?>>();

            Assert.IsType<GetVeiculoByIdHandler>(handler);
            Assert.NotNull(escopo.ServiceProvider.GetRequiredService<IDispatcher>());
        }

        [Fact]
        public void AddInfrastructure_SemConnectionString_DeveLancarDizendoOQueFalta()
        {
            var valores = Composicao.ConfiguracaoMinima();
            valores.Remove("ConnectionStrings:DefaultConnection");

            var erro = Assert.Throws<InvalidOperationException>(() => Composicao.Servicos(valores));

            Assert.Contains("ConnectionStrings:DefaultConnection", erro.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("curta-demais")]
        public void AddInfrastructure_ChaveJwtAusenteOuCurta_DeveLancar(string? chave)
        {
            var valores = Composicao.ConfiguracaoMinima();
            valores["Jwt:Key"] = chave;

            var erro = Assert.Throws<InvalidOperationException>(() => Composicao.Servicos(valores));

            Assert.Contains("Jwt:Key", erro.Message);
        }

        [Fact]
        public void AddInfrastructure_ProducaoSemConfiguracaoObrigatoria_DeveListarTudoQueFalta()
        {
            var valores = Composicao.ConfiguracaoMinima();
            valores["ASPNETCORE_ENVIRONMENT"] = "Production";

            var erro = Assert.Throws<InvalidOperationException>(() => Composicao.Servicos(valores));

            Assert.Contains("Cors__AllowedOrigins__0", erro.Message);
            Assert.Contains("Frontend__BaseUrl", erro.Message);
            Assert.Contains("Backoffice__ApiKey", erro.Message);
        }

        [Fact]
        public void AddInfrastructure_ProducaoCompleta_DeveMontarSemErro()
        {
            var valores = Composicao.ConfiguracaoMinima();
            valores["ASPNETCORE_ENVIRONMENT"] = "Production";
            valores["Cors:AllowedOrigins:0"] = "https://app.frota360app.com.br";
            valores["Frontend:BaseUrl"] = "https://app.frota360app.com.br";
            valores["Backoffice:ApiKey"] = "chave-do-backoffice";

            using var provedor = Composicao.Provedor(valores);

            Assert.NotNull(provedor.GetRequiredService<IEmailService>());
        }

        [Fact]
        public void AddInfrastructure_SemChaveDoResend_DeveSoLogarOsEmails()
        {
            using var provedor = Composicao.Provedor();

            Assert.IsType<LogEmailService>(provedor.GetRequiredService<IEmailService>());
        }

        [Fact]
        public void AddInfrastructure_ComChaveDoResend_DeveEnviarPeloResend()
        {
            var valores = Composicao.ConfiguracaoMinima();
            valores["Resend:ApiKey"] = "re_chave";

            using var provedor = Composicao.Provedor(valores);

            Assert.IsType<ResendEmailService>(provedor.GetRequiredService<IEmailService>());
        }

        [Fact]
        public void AddInfrastructure_DeveValidarEmissorAudienciaValidadeEAssinaturaDoJwt()
        {
            var parametros = OpcoesJwt(Composicao.Provedor()).TokenValidationParameters;

            Assert.True(parametros.ValidateIssuer);
            Assert.True(parametros.ValidateAudience);
            Assert.True(parametros.ValidateLifetime);
            Assert.True(parametros.ValidateIssuerSigningKey);
            Assert.Equal(Composicao.Emissor, parametros.ValidIssuer);
            Assert.Equal(Composicao.Audiencia, parametros.ValidAudience);
        }

        [Fact]
        public async Task OnMessageReceived_SemHeader_DeveLerOTokenDoCookie()
        {
            var opcoes = OpcoesJwt(Composicao.Provedor());
            var http = new DefaultHttpContext();
            http.Request.Headers.Cookie = $"{CookiesDeSessao.Token}=token-do-cookie";
            var contexto = new MessageReceivedContext(http, Esquema(), opcoes);

            await opcoes.Events.OnMessageReceived(contexto);

            Assert.Equal("token-do-cookie", contexto.Token);
        }

        [Fact]
        public async Task OnMessageReceived_TokenJaInformado_DevePrevalecerSobreOCookie()
        {
            var opcoes = OpcoesJwt(Composicao.Provedor());
            var http = new DefaultHttpContext();
            http.Request.Headers.Cookie = $"{CookiesDeSessao.Token}=token-do-cookie";
            var contexto = new MessageReceivedContext(http, Esquema(), opcoes) { Token = "token-do-header" };

            await opcoes.Events.OnMessageReceived(contexto);

            Assert.Equal("token-do-header", contexto.Token);
        }

        [Fact]
        public async Task OnMessageReceived_SemHeaderNemCookie_DeveDeixarSemToken()
        {
            var opcoes = OpcoesJwt(Composicao.Provedor());
            var contexto = new MessageReceivedContext(new DefaultHttpContext(), Esquema(), opcoes);

            await opcoes.Events.OnMessageReceived(contexto);

            Assert.Null(contexto.Token);
        }

        [Fact]
        public async Task OnChallenge_DeveResponder401NoEnvelopeDaApi()
        {
            var opcoes = OpcoesJwt(Composicao.Provedor());
            var http = HttpComCorpo();
            var contexto = new JwtBearerChallengeContext(http, Esquema(), opcoes, new AuthenticationProperties());

            await opcoes.Events.OnChallenge(contexto);

            Assert.True(contexto.Handled);
            Assert.Equal(StatusCodes.Status401Unauthorized, http.Response.StatusCode);
            await AssertEnvelopeDeFalha(http, "Não autenticado. Faça login para continuar.");
        }

        [Fact]
        public async Task OnForbidden_DeveResponder403NoEnvelopeDaApi()
        {
            var opcoes = OpcoesJwt(Composicao.Provedor());
            var http = HttpComCorpo();

            await opcoes.Events.OnForbidden(new ForbiddenContext(http, Esquema(), opcoes));

            Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);
            await AssertEnvelopeDeFalha(http, "Você não tem permissão para esta operação.");
        }

        private static JwtBearerOptions OpcoesJwt(IServiceProvider provedor) =>
            provedor.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        private static AuthenticationScheme Esquema() =>
            new(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));

        private static DefaultHttpContext HttpComCorpo()
        {
            var http = new DefaultHttpContext();
            http.Response.Body = new MemoryStream();
            return http;
        }

        private static async Task AssertEnvelopeDeFalha(DefaultHttpContext http, string mensagem)
        {
            Assert.Equal("application/json", http.Response.ContentType);

            http.Response.Body.Position = 0;
            using var json = await JsonDocument.ParseAsync(http.Response.Body);

            Assert.False(json.RootElement.GetProperty("sucesso").GetBoolean());
            Assert.Equal(mensagem, json.RootElement.GetProperty("mensagem").GetString());
        }
    }
}
