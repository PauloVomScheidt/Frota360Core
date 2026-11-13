using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Veiculo.Request;
using Frota360.Application.DTOs.Veiculo.Response;
using Frota360.Application.UseCases.Veiculos.Commands.CreateVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.DeleteVeiculo;
using Frota360.Application.UseCases.Veiculos.Commands.UpdateVeiculo;
using Frota360.Application.UseCases.Veiculos.Queries.GetAllVeiculos;
using Frota360.Application.UseCases.Veiculos.Queries.GetVeiculoById;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class VeiculoControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private VeiculoController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreateVeiculoRequest>(), Validacao.Aceita<UpdateVeiculoRequest>())
            : new(_dispatcher, Validacao.Recusa<CreateVeiculoRequest>(), Validacao.Recusa<UpdateVeiculoRequest>());

        [Fact]
        public async Task GetAll_DeveDevolverAFrota()
        {
            IEnumerable<VeiculoResponse> veiculos = [new() { Id = 1 }];
            _dispatcher.SendAsync(Arg.Any<GetAllVeiculosQuery>()).Returns(veiculos);

            Assert.Same(veiculos, Resposta.Ok<IEnumerable<VeiculoResponse>>(await Controller().GetAll()));
        }

        [Fact]
        public async Task GetById_DeveDevolverOVeiculoOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetVeiculoByIdQuery>(q => q.Id == 3)).Returns(new VeiculoResponse { Id = 3 });

            Assert.Equal(3, Resposta.Ok<VeiculoResponse>(await Controller().GetById(3)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(4), "Veículo 4 não encontrado.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreateVeiculoRequest();
            _dispatcher.SendAsync(Arg.Is<CreateVeiculoCommand>(c => c.Data == request)).Returns(new VeiculoResponse { Id = 3 });

            Resposta.Criado<VeiculoResponse>(await Controller().Create(request), 3, "Veículo cadastrado com sucesso.");
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreateVeiculoRequest()));
            Resposta.Invalido(await controller.Update(3, new UpdateVeiculoRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverOVeiculoOu404()
        {
            var request = new UpdateVeiculoRequest();
            _dispatcher.SendAsync(Arg.Is<UpdateVeiculoCommand>(c => c.Id == 3 && c.Data == request)).Returns(new VeiculoResponse { Id = 3 });

            Resposta.Ok<VeiculoResponse>(await Controller().Update(3, request), "Veículo atualizado com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(4, request), "Veículo 4 não encontrado.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeleteVeiculoCommand>(c => c.Id == 3)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(3), "Veículo removido com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(4), "Veículo 4 não encontrado.");
        }
    }
}
