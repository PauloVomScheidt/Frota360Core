using Frota360.Application.DTOs.Motorista.Request;
using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Motoristas.Commands.CreateMotorista;
using Frota360.Application.UseCases.Motoristas.Commands.DeleteMotorista;
using Frota360.Application.UseCases.Motoristas.Commands.UpdateMotorista;
using Frota360.Application.UseCases.Motoristas.Queries.GetAllMotoristas;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Motoristas
{
    public class MotoristaHandlersTests
    {
        private readonly IMotoristaRepository _repository = Substitute.For<IMotoristaRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

        public MotoristaHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private static Motorista NovoMotorista(int id = 1) => new()
        {
            Id = id,
            Nome = "João da Silva",
            Email = "joao@email.com",
            CPF = "39053344705",
            DataNascimento = new DateTime(1990, 1, 1),
            DataInclusao = new DateTime(2024, 1, 1)
        };

        // ----- Create -----

        [Fact]
        public async Task Create_DevePersistirEMapearResposta()
        {
            _repository.AddAsync(Arg.Any<Motorista>())
                .Returns(ci =>
                {
                    var m = ci.Arg<Motorista>();
                    m.Id = 10;
                    return m;
                });

            var handler = new CreateMotoristaHandler(_repository, _currentUser, NullLogger<CreateMotoristaHandler>.Instance);
            var request = new CreateMotoristaRequest
            {
                Nome = "Maria",
                Email = "maria@email.com",
                CPF = "39053344705",
                DataNascimento = new DateTime(1995, 5, 20)
            };

            var resposta = await handler.HandleAsync(new CreateMotoristaCommand(request));

            Assert.Equal(10, resposta.Id);
            Assert.Equal("Maria", resposta.Nome);
            Assert.Equal("maria@email.com", resposta.Email);
            Assert.Equal("39053344705", resposta.CPF);
            await _repository.Received(1).AddAsync(Arg.Is<Motorista>(m =>
                m.Nome == "Maria" && m.EmpresaId == 1 && m.DataInclusao != default));
        }

        // ----- Update -----

        [Fact]
        public async Task Update_QuandoExiste_DeveAtualizarERetornarResposta()
        {
            var existente = NovoMotorista(7);
            _repository.GetByIdAsync(7, 1).Returns(existente);
            _repository.UpdateAsync(Arg.Any<Motorista>()).Returns(ci => ci.Arg<Motorista>());

            var handler = new UpdateMotoristaHandler(_repository, _currentUser, NullLogger<UpdateMotoristaHandler>.Instance);
            var request = new UpdateMotoristaRequest
            {
                Nome = "Nome Alterado",
                Email = "novo@email.com",
                CPF = "39053344705",
                DataNascimento = new DateTime(1988, 3, 3)
            };

            var resposta = await handler.HandleAsync(new UpdateMotoristaCommand(7, request));

            Assert.NotNull(resposta);
            Assert.Equal(7, resposta!.Id);
            Assert.Equal("Nome Alterado", resposta.Nome);
            Assert.Equal("novo@email.com", resposta.Email);
            await _repository.Received(1).UpdateAsync(Arg.Any<Motorista>());
        }

        [Fact]
        public async Task Update_QuandoNaoExiste_DeveRetornarNull_ENaoChamarUpdate()
        {
            _repository.GetByIdAsync(99, 1).Returns((Motorista?)null);

            var handler = new UpdateMotoristaHandler(_repository, _currentUser, NullLogger<UpdateMotoristaHandler>.Instance);

            var resposta = await handler.HandleAsync(
                new UpdateMotoristaCommand(99, new UpdateMotoristaRequest()));

            Assert.Null(resposta);
            await _repository.DidNotReceive().UpdateAsync(Arg.Any<Motorista>());
        }

        // ----- Delete -----

        [Fact]
        public async Task Delete_QuandoExiste_DeveRemoverERetornarTrue()
        {
            var existente = NovoMotorista(5);
            _repository.GetByIdAsync(5, 1).Returns(existente);

            var handler = new DeleteMotoristaHandler(_repository, _currentUser, NullLogger<DeleteMotoristaHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteMotoristaCommand(5));

            Assert.True(resultado);
            await _repository.Received(1).DeleteAsync(existente);
        }

        [Fact]
        public async Task Delete_QuandoNaoExiste_DeveRetornarFalse_ENaoRemover()
        {
            _repository.GetByIdAsync(123, 1).Returns((Motorista?)null);

            var handler = new DeleteMotoristaHandler(_repository, _currentUser, NullLogger<DeleteMotoristaHandler>.Instance);

            var resultado = await handler.HandleAsync(new DeleteMotoristaCommand(123));

            Assert.False(resultado);
            await _repository.DidNotReceive().DeleteAsync(Arg.Any<Motorista>());
        }

        // ----- GetAll -----

        [Fact]
        public async Task GetAll_DeveMapearTodosOsMotoristas()
        {
            _repository.GetAllAsync(1).Returns(new[] { NovoMotorista(1), NovoMotorista(2) });

            var handler = new GetAllMotoristasHandler(_repository, _currentUser, NullLogger<GetAllMotoristasHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllMotoristasQuery())).ToList();

            Assert.Equal(2, resposta.Count);
            Assert.Collection(resposta,
                m => Assert.Equal(1, m.Id),
                m => Assert.Equal(2, m.Id));
        }
    }
}
