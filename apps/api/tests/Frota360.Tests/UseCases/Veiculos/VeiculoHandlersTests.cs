using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.UseCases.Veiculos.Commands.CreateVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.UpdateVeiculo;
using Frota360.Application.UseCases.Veiculos.Queries.GetAllVeiculos;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Veiculos
{
    public class VeiculoHandlersTests
    {
        private readonly IVeiculoRepository _repository = Substitute.For<IVeiculoRepository>();

        private static Veiculo NovoVeiculo(int id = 1) => new()
        {
            Id = id,
            NomeVeiculo = "Strada",
            MarcaVeiculo = "Fiat",
            Placa = "ABC1D23",
            Quilometragem = 50_000,
            DataInclusao = new DateTime(2024, 1, 1)
        };

        [Fact]
        public async Task Create_DevePersistirEMapearResposta()
        {
            _repository.AddAsync(Arg.Any<Veiculo>())
                .Returns(ci =>
                {
                    var v = ci.Arg<Veiculo>();
                    v.Id = 3;
                    return v;
                });

            var handler = new CreateVeiculoHandler(_repository, NullLogger<CreateVeiculoHandler>.Instance);
            var request = new CreateVeiculoRequest
            {
                NomeVeiculo = "Saveiro",
                MarcaVeiculo = "VW",
                Placa = "XYZ9K88",
                Quilometragem = 1200
            };

            var resposta = await handler.HandleAsync(new CreateVeiculoCommand(request));

            Assert.Equal(3, resposta.Id);
            Assert.Equal("Saveiro", resposta.NomeVeiculo);
            Assert.Equal("XYZ9K88", resposta.Placa);
            await _repository.Received(1).AddAsync(Arg.Is<Veiculo>(v => v.DataInclusao != default));
        }

        [Fact]
        public async Task Update_QuandoExiste_DeveAtualizarERetornarResposta()
        {
            _repository.GetByIdAsync(7).Returns(NovoVeiculo(7));
            _repository.UpdateAsync(Arg.Any<Veiculo>()).Returns(ci => ci.Arg<Veiculo>());

            var handler = new UpdateVeiculoHandler(_repository, NullLogger<UpdateVeiculoHandler>.Instance);
            var request = new UpdateVeiculoRequest
            {
                NomeVeiculo = "Strada Volcano",
                MarcaVeiculo = "Fiat",
                Placa = "ABC1D23",
                Quilometragem = 60_000
            };

            var resposta = await handler.HandleAsync(new UpdateVeiculoCommand(7, request));

            Assert.NotNull(resposta);
            Assert.Equal("Strada Volcano", resposta!.NomeVeiculo);
            Assert.Equal(60_000, resposta.Quilometragem);
            await _repository.Received(1).UpdateAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task Update_QuandoNaoExiste_DeveRetornarNull()
        {
            _repository.GetByIdAsync(99).Returns((Veiculo?)null);

            var handler = new UpdateVeiculoHandler(_repository, NullLogger<UpdateVeiculoHandler>.Instance);

            var resposta = await handler.HandleAsync(
                new UpdateVeiculoCommand(99, new UpdateVeiculoRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task Delete_QuandoExiste_DeveRemoverERetornarTrue()
        {
            var existente = NovoVeiculo(4);
            _repository.GetByIdAsync(4).Returns(existente);

            var handler = new DeleteVeiculoHandler(_repository, NullLogger<DeleteVeiculoHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteVeiculoCommand(4));

            Assert.True(resultado);
            await _repository.Received(1).DeleteAsync(existente);
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarFalse()
        {
            _repository.GetByIdAsync(123).Returns((Veiculo?)null);

            var handler = new DeleteVeiculoHandler(_repository, NullLogger<DeleteVeiculoHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteVeiculoCommand(123));

            Assert.False(resultado);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Veiculo>());
        }

        [Fact]
        public async Task GetAll_DeveMapearTodosOsVeiculos()
        {
            _repository.GetAllAsync().Returns(new[] { NovoVeiculo(1), NovoVeiculo(2) });

            var handler = new GetAllVeiculosHandler(_repository, NullLogger<GetAllVeiculosHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllVeiculosQuery())).ToList();

            Assert.Equal(2, resposta.Count);
        }
    }
}
