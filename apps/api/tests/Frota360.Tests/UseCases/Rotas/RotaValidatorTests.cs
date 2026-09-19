using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Rotas.Validator;
using Frota360.Domain.Common;
using NSubstitute;

namespace Frota360.Tests.UseCases.Rotas
{
    public class RotaValidatorTests
    {
        // O validador lê a role para saber se o CodigoMotorista é exigível; por padrão
        // a requisição é de gestão (Role null nunca é "Motorista").
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
        private readonly CreateRotaValidator _createValidator;
        private readonly UpdateRotaValidator _updateValidator = new();
        private readonly EncerrarRotaValidator _encerrarValidator = new();
        private readonly CalcularRotaValidator _calcularValidator = new();

        public RotaValidatorTests() => _createValidator = new CreateRotaValidator(_currentUser);

        private static CreateRotaRequest CreateValido() => new()
        {
            EnderecoPartida = "Joinville",
            EnderecoChegada = "Blumenau",
            CodigoMotorista = 2,
            CodigoVeiculo = 3,
            DataInicio = new DateTime(2025, 6, 1),
            KmInicial = 50_000
        };

        [Fact]
        public void Create_RequestValido_DevePassar()
        {
            var resultado = _createValidator.Validate(CreateValido());
            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Create_SemMotorista_DeveFalhar()
        {
            var request = CreateValido();
            request.CodigoMotorista = 0;

            var resultado = _createValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.CodigoMotorista));
        }

        [Fact]
        public void Create_ComoMotorista_NaoDeveExigirCodigoMotorista()
        {
            // Ele não escolhe o motorista da rota — o handler grava o da claim.
            _currentUser.Role.Returns(Roles.Motorista);
            var request = CreateValido();
            request.CodigoMotorista = 0;

            var resultado = _createValidator.Validate(request);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-50_000)]
        [InlineData(2_000_001)]
        public void Create_KmInicialInvalido_DeveFalhar(int kmInicial)
        {
            var request = CreateValido();
            request.KmInicial = kmInicial;

            var resultado = _createValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.KmInicial));
        }

        [Fact]
        public void Create_KmInicialZero_DevePassar()
        {
            var request = CreateValido();
            request.KmInicial = 0;

            var resultado = _createValidator.Validate(request);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Encerrar_RequestValido_DevePassar()
        {
            var resultado = _encerrarValidator.Validate(
                new EncerrarRotaRequest { KmFinal = 50_430, DataFim = DateTime.Now });

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Encerrar_SemDataFim_DevePassar()
        {
            var resultado = _encerrarValidator.Validate(new EncerrarRotaRequest { KmFinal = 50_430 });

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2_000_001)]
        public void Encerrar_KmFinalInvalido_DeveFalhar(int kmFinal)
        {
            var request = new EncerrarRotaRequest { KmFinal = kmFinal };

            var resultado = _encerrarValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.KmFinal));
        }

        [Fact]
        public void Encerrar_DataFimNoFuturo_DeveFalhar()
        {
            var request = new EncerrarRotaRequest
            {
                KmFinal = 50_430,
                DataFim = DateTime.Now.AddDays(5)
            };

            var resultado = _encerrarValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.DataFim));
        }

        private static CalcularRotaRequest CalcularValido() => new()
        {
            LatitudeOrigem = -25.4284m,
            LongitudeOrigem = -49.2733m,
            LatitudeDestino = -23.5505m,
            LongitudeDestino = -46.6333m
        };

        [Fact]
        public void Calcular_RequestValido_DevePassar()
        {
            var resultado = _calcularValidator.Validate(CalcularValido());
            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(-90.001)]
        [InlineData(90.001)]
        public void Calcular_LatitudeForaDaFaixa_DeveFalhar(double latitude)
        {
            var request = CalcularValido();
            request.LatitudeOrigem = (decimal)latitude;

            var resultado = _calcularValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.LatitudeOrigem));
        }

        [Theory]
        [InlineData(-180.001)]
        [InlineData(180.001)]
        public void Calcular_LongitudeForaDaFaixa_DeveFalhar(double longitude)
        {
            var request = CalcularValido();
            request.LongitudeDestino = (decimal)longitude;

            var resultado = _calcularValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(request.LongitudeDestino));
        }

        [Theory]
        [InlineData(-90)]
        [InlineData(90)]
        [InlineData(0)]
        public void Calcular_LatitudeNoLimite_DevePassar(double latitude)
        {
            var request = CalcularValido();
            request.LatitudeOrigem = (decimal)latitude;

            Assert.True(_calcularValidator.Validate(request).IsValid);
        }

        [Fact]
        public void Calcular_OrigemIgualAoDestino_DeveFalhar()
        {
            var request = CalcularValido();
            request.LatitudeDestino = request.LatitudeOrigem;
            request.LongitudeDestino = request.LongitudeOrigem;

            var resultado = _calcularValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("mesmo ponto"));
        }

        [Fact]
        public void Calcular_MesmaLatitudeComLongitudeDiferente_DevePassar()
        {
            var request = CalcularValido();
            request.LatitudeDestino = request.LatitudeOrigem;

            Assert.True(_calcularValidator.Validate(request).IsValid);
        }

        // ----- Plausibilidade do traçado (distância × duração) -----
        //
        // Distância e duração chegam do CLIENTE: o front já pagou pela chamada à Routes API e
        // reenviá-la no salvamento custaria uma segunda cobrança. Esta trava é a contrapartida.

        /// <summary>131 km em 2h02 — o trajeto Joinville→Curitiba real, a ~64 km/h.</summary>
        [Fact]
        public void Create_TracadoComVelocidadePlausivel_DevePassar()
        {
            var request = CreateValido();
            request.DistanciaMetros = 130_797;
            request.DuracaoEstimadaSegundos = 7_342;

            Assert.True(_createValidator.Validate(request).IsValid);
        }

        [Theory]
        // 400 km em 20 min = 1.200 km/h: distância inflada.
        [InlineData(400_000, 1_200)]
        // 2 km em 4 h = 0,5 km/h: duração absurda para a distância.
        [InlineData(2_000, 14_400)]
        // Distância sem tempo nenhum seria velocidade infinita.
        [InlineData(130_797, 0)]
        public void Create_TracadoComVelocidadeImplausivel_DeveFalhar(int metros, int segundos)
        {
            var request = CreateValido();
            request.DistanciaMetros = metros;
            request.DuracaoEstimadaSegundos = segundos;

            var resultado = _createValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("não são compatíveis"));
        }

        /// <summary>A mensagem vai literalmente ao usuário — não pode carregar o número da checagem.</summary>
        [Fact]
        public void Create_TracadoImplausivel_MensagemNaoVazaDetalheTecnico()
        {
            var request = CreateValido();
            request.DistanciaMetros = 400_000;
            request.DuracaoEstimadaSegundos = 1_200;

            var erro = _createValidator.Validate(request).Errors
                .First(e => e.ErrorMessage.Contains("não são compatíveis"));

            Assert.DoesNotContain("km/h", erro.ErrorMessage);
            Assert.DoesNotContain("velocidade", erro.ErrorMessage, StringComparison.OrdinalIgnoreCase);

            // O controller monta "{PropertyName}: {ErrorMessage}" — o nome do campo também
            // vai à tela, então ele não pode ser o identificador interno.
            Assert.Equal("Trajeto", erro.PropertyName);
        }

        [Fact]
        public void Create_SemTracado_NaoDeveDispararAChecagem()
        {
            var request = CreateValido();
            request.DistanciaMetros = null;
            request.DuracaoEstimadaSegundos = null;

            Assert.True(_createValidator.Validate(request).IsValid);
        }

        [Theory]
        // Só um dos dois: a regra exige o par completo.
        [InlineData(130_797, null)]
        [InlineData(null, 7_342)]
        public void Create_TracadoIncompleto_NaoDeveDispararAChecagem(int? metros, int? segundos)
        {
            var request = CreateValido();
            request.DistanciaMetros = metros;
            request.DuracaoEstimadaSegundos = segundos;

            Assert.True(_createValidator.Validate(request).IsValid);
        }

        /// <summary>Zero é o valor de "sem traçado", não uma medida a julgar.</summary>
        [Fact]
        public void Create_DistanciaZerada_NaoDeveDispararAChecagem()
        {
            var request = CreateValido();
            request.DistanciaMetros = 0;
            request.DuracaoEstimadaSegundos = 0;

            Assert.True(_createValidator.Validate(request).IsValid);
        }

        /// <summary>A mesma regra vale no update — as duas telas salvam pelo mesmo caminho.</summary>
        [Fact]
        public void Update_TracadoComVelocidadeImplausivel_DeveFalhar()
        {
            var request = new UpdateRotaRequest
            {
                EnderecoPartida = "Joinville",
                EnderecoChegada = "Blumenau",
                CodigoMotorista = 1,
                CodigoVeiculo = 1,
                DataInicio = new DateTime(2025, 6, 1),
                DistanciaMetros = 400_000,
                DuracaoEstimadaSegundos = 1_200,
            };

            var resultado = _updateValidator.Validate(request);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("não são compatíveis"));
        }
    }
}
