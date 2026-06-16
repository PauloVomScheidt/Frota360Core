using Frota360.Application.DTOs.Rota.Request;
using Frota360.Application.UseCases.Rotas.Commands.CreateRota;
using Frota360.Application.UseCases.Rotas.Commands.DeleteRota;
using Frota360.Application.UseCases.Rotas.Commands.UpdateRota;
using Frota360.Application.UseCases.Rotas.Queries.GetAllRotas;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Rotas
{
    public class RotaHandlersTests
    {
        private readonly IRotaRepository _repository = Substitute.For<IRotaRepository>();

        private static Rota NovaRota(int id = 1) => new()
        {
            Id = id,
            Origem = "Curitiba",
            Destino = "São Paulo",
            CodigoMotorista = 1,
            CodigoVeiculo = 1,
            Ativo = true,
            DataInicio = new DateTime(2024, 1, 1),
            DataInclusao = new DateTime(2024, 1, 1)
        };

        [Fact]
        public async Task Create_DevePersistirEMapearResposta()
        {
            _repository.AddAsync(Arg.Any<Rota>())
                .Returns(ci =>
                {
                    var r = ci.Arg<Rota>();
                    r.Id = 8;
                    return r;
                });

            var handler = new CreateRotaHandler(_repository, NullLogger<CreateRotaHandler>.Instance);
            var request = new CreateRotaRequest
            {
                Origem = "Joinville",
                Destino = "Blumenau",
                CodigoMotorista = 2,
                CodigoVeiculo = 3,
                Ativo = true,
                DataInicio = new DateTime(2025, 6, 1)
            };

            var resposta = await handler.HandleAsync(new CreateRotaCommand(request));

            Assert.Equal(8, resposta.Id);
            Assert.Equal("Joinville", resposta.Origem);
            Assert.Equal("Blumenau", resposta.Destino);
            await _repository.Received(1).AddAsync(Arg.Is<Rota>(r => r.DataInclusao != default));
        }

        [Fact]
        public async Task Update_QuandoExiste_DeveAtualizarERetornarResposta()
        {
            _repository.GetByIdAsync(7).Returns(NovaRota(7));
            _repository.UpdateAsync(Arg.Any<Rota>()).Returns(ci => ci.Arg<Rota>());

            var handler = new UpdateRotaHandler(_repository, NullLogger<UpdateRotaHandler>.Instance);
            var request = new UpdateRotaRequest
            {
                Origem = "Curitiba",
                Destino = "Florianópolis",
                CodigoMotorista = 1,
                CodigoVeiculo = 1,
                Ativo = false,
                DataInicio = new DateTime(2024, 1, 1)
            };

            var resposta = await handler.HandleAsync(new UpdateRotaCommand(7, request));

            Assert.NotNull(resposta);
            Assert.Equal("Florianópolis", resposta!.Destino);
            Assert.False(resposta.Ativo);
            await _repository.Received(1).UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Update_QuandoNaoExiste_DeveRetornarNull()
        {
            _repository.GetByIdAsync(99).Returns((Rota?)null);

            var handler = new UpdateRotaHandler(_repository, NullLogger<UpdateRotaHandler>.Instance);

            var resposta = await handler.HandleAsync(
                new UpdateRotaCommand(99, new UpdateRotaRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task Delete_QuandoExiste_DeveRemoverERetornarTrue()
        {
            var existente = NovaRota(6);
            _repository.GetByIdAsync(6).Returns(existente);

            var handler = new DeleteRotaHandler(_repository, NullLogger<DeleteRotaHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteRotaCommand(6));

            Assert.True(resultado);
            await _repository.Received(1).DeleteAsync(existente);
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarFalse()
        {
            _repository.GetByIdAsync(123).Returns((Rota?)null);

            var handler = new DeleteRotaHandler(_repository, NullLogger<DeleteRotaHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteRotaCommand(123));

            Assert.False(resultado);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Rota>());
        }

        [Fact]
        public async Task GetAll_DeveMapearTodasAsRotas()
        {
            _repository.GetAllAsync().Returns(new[] { NovaRota(1), NovaRota(2), NovaRota(3) });

            var handler = new GetAllRotasHandler(_repository, NullLogger<GetAllRotasHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllRotasQuery())).ToList();

            Assert.Equal(3, resposta.Count);
        }
    }
}
