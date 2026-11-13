using Frota360.Api.Controllers;
using Frota360.Application.Abstractions.Messaging;
using Frota360.Application.DTOs.Posto.Request;
using Frota360.Application.DTOs.Posto.Response;
using Frota360.Application.UseCases.Postos.Commands.CreatePosto;
using Frota360.Application.UseCases.Postos.Commands.DeletePosto;
using Frota360.Application.UseCases.Postos.Commands.UpdatePosto;
using Frota360.Application.UseCases.Postos.Queries.GetAllPostos;
using Frota360.Application.UseCases.Postos.Queries.GetPostoById;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    public class PostoControllerTests
    {
        private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

        private PostoController Controller(bool valido = true) => valido
            ? new(_dispatcher, Validacao.Aceita<CreatePostoRequest>(), Validacao.Aceita<UpdatePostoRequest>())
            : new(_dispatcher, Validacao.Recusa<CreatePostoRequest>(), Validacao.Recusa<UpdatePostoRequest>());

        [Fact]
        public async Task GetAll_DeveRepassarOFiltroDeAtivos()
        {
            IEnumerable<PostoResponse> postos = [new() { Id = 1 }];
            _dispatcher.SendAsync(Arg.Is<GetAllPostosQuery>(q => q.ApenasAtivos)).Returns(postos);

            Assert.Same(postos, Resposta.Ok<IEnumerable<PostoResponse>>(await Controller().GetAll(apenasAtivos: true)));
        }

        [Fact]
        public async Task GetById_DeveDevolverOPostoOu404()
        {
            _dispatcher.SendAsync(Arg.Is<GetPostoByIdQuery>(q => q.Id == 3)).Returns(new PostoResponse { Id = 3 });

            Assert.Equal(3, Resposta.Ok<PostoResponse>(await Controller().GetById(3)).Id);
            Resposta.NaoEncontrado(await Controller().GetById(4), "Posto 4 não encontrado.");
        }

        [Fact]
        public async Task Create_DeveResponder201()
        {
            var request = new CreatePostoRequest();
            _dispatcher.SendAsync(Arg.Is<CreatePostoCommand>(c => c.Data == request)).Returns(new PostoResponse { Id = 3 });

            Resposta.Criado<PostoResponse>(await Controller().Create(request), 3, "Posto cadastrado com sucesso.");
        }

        [Fact]
        public async Task Escrita_ComDadosInvalidos_DeveResponder400SemDespachar()
        {
            var controller = Controller(valido: false);

            Resposta.Invalido(await controller.Create(new CreatePostoRequest()));
            Resposta.Invalido(await controller.Update(3, new UpdatePostoRequest()));
            Assert.Empty(_dispatcher.ReceivedCalls());
        }

        [Fact]
        public async Task Update_DeveDevolverOPostoOu404()
        {
            var request = new UpdatePostoRequest();
            _dispatcher.SendAsync(Arg.Is<UpdatePostoCommand>(c => c.Id == 3 && c.Data == request)).Returns(new PostoResponse { Id = 3 });

            Resposta.Ok<PostoResponse>(await Controller().Update(3, request), "Posto atualizado com sucesso.");
            Resposta.NaoEncontrado(await Controller().Update(4, request), "Posto 4 não encontrado.");
        }

        [Fact]
        public async Task Delete_DeveConfirmarOu404()
        {
            _dispatcher.SendAsync(Arg.Is<DeletePostoCommand>(c => c.Id == 3)).Returns(true);

            Resposta.OkSemDados(await Controller().Delete(3), "Posto removido com sucesso.");
            Resposta.NaoEncontrado(await Controller().Delete(4), "Posto 4 não encontrado.");
        }
    }
}
