using FluentValidation;
using FluentValidation.Results;
using Frota360.Api.Controllers;
using Frota360.Application.DTOs.Backoffice.Request;
using Frota360.Application.DTOs.Backoffice.Response;
using Frota360.Application.Interfaces;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class BackofficeControllerTests
    {
        private const string ChaveConfigurada = "chave-do-backoffice";

        private readonly IBackofficeService _backofficeService = Substitute.For<IBackofficeService>();
        private readonly IValidator<ProvisionarEmpresaRequest> _validator = Substitute.For<IValidator<ProvisionarEmpresaRequest>>();

        public BackofficeControllerTests() =>
            _validator.ValidateAsync(Arg.Any<ProvisionarEmpresaRequest>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult());

        private BackofficeController CriarController(string? chaveConfigurada = ChaveConfigurada)
        {
            var configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Backoffice:ApiKey"] = chaveConfigurada })
                .Build();

            return new BackofficeController(_backofficeService, configuracao, _validator);
        }

        private static ProvisionarEmpresaRequest NovoRequest() =>
            new() { NomeEmpresa = "Transportes Exemplo", EmailAdmin = "admin@exemplo.com" };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("chave-errada")]
        public async Task ProvisionarEmpresa_ChaveAusenteOuErrada_DeveResponder401SemProvisionar(string? chaveInformada)
        {
            var resultado = await CriarController().ProvisionarEmpresa(NovoRequest(), chaveInformada);

            Assert.IsType<UnauthorizedObjectResult>(resultado);
            await _backofficeService.DidNotReceiveWithAnyArgs().ProvisionarEmpresaAsync(default!);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ProvisionarEmpresa_SemChaveConfigurada_DeveResponder401MesmoComHeaderVazio(string? chaveConfigurada)
        {
            var resultado = await CriarController(chaveConfigurada).ProvisionarEmpresa(NovoRequest(), chaveConfigurada);

            Assert.IsType<UnauthorizedObjectResult>(resultado);
            await _backofficeService.DidNotReceiveWithAnyArgs().ProvisionarEmpresaAsync(default!);
        }

        [Fact]
        public async Task ProvisionarEmpresa_RequestInvalido_DeveResponder400ComOsErros()
        {
            _validator.ValidateAsync(Arg.Any<ProvisionarEmpresaRequest>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult([new ValidationFailure("EmailAdmin", "E-mail inválido.")]));

            var resultado = await CriarController().ProvisionarEmpresa(NovoRequest(), ChaveConfigurada);

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
            var corpo = Assert.IsType<ApiResponse<object>>(badRequest.Value);
            Assert.Contains("EmailAdmin: E-mail inválido.", corpo.Erros!);
            await _backofficeService.DidNotReceiveWithAnyArgs().ProvisionarEmpresaAsync(default!);
        }

        [Fact]
        public async Task ProvisionarEmpresa_ChaveCorreta_DeveProvisionarEResponder201()
        {
            var request = NovoRequest();
            _backofficeService.ProvisionarEmpresaAsync(request).Returns(new EmpresaProvisionadaResponse
            {
                EmpresaId = 7,
                NomeEmpresa = request.NomeEmpresa,
                EmailAdmin = request.EmailAdmin,
                LinkConvite = "https://app.exemplo/convite?token=abc"
            });

            var resultado = await CriarController().ProvisionarEmpresa(request, ChaveConfigurada);

            var criado = Assert.IsType<CreatedResult>(resultado);
            var corpo = Assert.IsType<ApiResponse<EmpresaProvisionadaResponse>>(criado.Value);
            Assert.Equal(7, corpo.Dados!.EmpresaId);
        }
    }
}
