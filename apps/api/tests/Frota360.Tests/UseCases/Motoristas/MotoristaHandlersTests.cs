using Frota360.Application.Interfaces;
using Frota360.Application.UseCases.Motoristas.Queries.GetAllMotoristas;
using Frota360.Application.UseCases.Motoristas.Queries.GetMotoristaById;
using Frota360.Domain.Common;
using Frota360.Domain.Entities;
using Frota360.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Frota360.Tests.UseCases.Motoristas
{
    /// <summary>
    /// Motorista não é entidade: é um <see cref="Usuario"/> com a role Motorista, e a
    /// fatia só lê. Criar, editar e excluir acontecem pelo fluxo de convite/usuário.
    /// </summary>
    public class MotoristaHandlersTests
    {
        private readonly IUsuarioRepository _repository = Substitute.For<IUsuarioRepository>();
        private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

        public MotoristaHandlersTests()
        {
            _currentUser.EmpresaId.Returns(1);
        }

        private static Usuario NovoMotorista(int id = 1) => new()
        {
            Id = id,
            EmpresaId = 1,
            Nome = "João da Silva",
            Email = "joao@email.com",
            Role = Roles.Motorista,
            CPF = "39053344705",
            DataNascimento = new DateTime(1990, 1, 1),
            Ativo = true,
            DataInclusao = new DateTime(2024, 1, 1)
        };

        [Fact]
        public async Task GetAll_DeveConsultarEscopadoNaEmpresa_EMapearOsCamposOpcionais()
        {
            _repository.GetMotoristasByEmpresaAsync(1).Returns(new[] { NovoMotorista(1), NovoMotorista(2) });

            var handler = new GetAllMotoristasHandler(_repository, _currentUser, NullLogger<GetAllMotoristasHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllMotoristasQuery())).ToList();

            Assert.Equal(2, resposta.Count);
            Assert.Equal("39053344705", resposta[0].CPF);
            Assert.Equal(new DateTime(1990, 1, 1), resposta[0].DataNascimento);
            Assert.True(resposta[0].Ativo);
            await _repository.Received(1).GetMotoristasByEmpresaAsync(1);
        }

        [Fact]
        public async Task GetAll_SemDadosPessoais_DeveMapearNulos()
        {
            var semDados = NovoMotorista(3);
            semDados.CPF = null;
            semDados.DataNascimento = null;
            _repository.GetMotoristasByEmpresaAsync(1).Returns(new[] { semDados });

            var handler = new GetAllMotoristasHandler(_repository, _currentUser, NullLogger<GetAllMotoristasHandler>.Instance);

            var resposta = (await handler.HandleAsync(new GetAllMotoristasQuery())).Single();

            Assert.Null(resposta.CPF);
            Assert.Null(resposta.DataNascimento);
        }

        [Fact]
        public async Task GetById_QuandoNaoEhMotoristaDaEmpresa_DeveRetornarNull()
        {
            // O repositório já filtra empresa + role: os dois casos chegam aqui como null.
            _repository.GetMotoristaByIdAsync(99, 1).Returns((Usuario?)null);

            var handler = new GetMotoristaByIdHandler(_repository, _currentUser, NullLogger<GetMotoristaByIdHandler>.Instance);

            Assert.Null(await handler.HandleAsync(new GetMotoristaByIdQuery(99)));
            await _repository.Received(1).GetMotoristaByIdAsync(99, 1);
        }
    }
}
