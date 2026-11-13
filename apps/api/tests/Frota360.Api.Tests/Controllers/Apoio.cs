using FluentValidation;
using FluentValidation.Results;
using Frota360.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Frota360.Api.Tests.Controllers
{
    internal static class Validacao
    {
        public const string Erro = "Campo: valor inválido.";

        public static IValidator<T> Aceita<T>()
        {
            var validator = Substitute.For<IValidator<T>>();
            validator.ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
            return validator;
        }

        public static IValidator<T> Recusa<T>()
        {
            var validator = Substitute.For<IValidator<T>>();
            validator.ValidateAsync(Arg.Any<T>(), Arg.Any<CancellationToken>())
                .Returns(new ValidationResult([new ValidationFailure("Campo", "valor inválido.")]));
            return validator;
        }
    }

    /// <summary>
    /// Asserções sobre o envelope <see cref="ApiResponse{T}"/> que todo controller devolve.
    /// </summary>
    internal static class Resposta
    {
        public static ApiResponse<T> Envelope<T>(IActionResult resultado, int status)
        {
            var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
            Assert.Equal(status, objeto.StatusCode);
            return Assert.IsType<ApiResponse<T>>(objeto.Value);
        }

        public static T Ok<T>(IActionResult resultado, string? mensagem = null)
        {
            var envelope = Envelope<T>(resultado, StatusCodes.Status200OK);
            Assert.True(envelope.Sucesso);
            if (mensagem is not null)
                Assert.Equal(mensagem, envelope.Mensagem);
            return envelope.Dados!;
        }

        public static void OkSemDados(IActionResult resultado, string mensagem)
        {
            var envelope = Envelope<object>(resultado, StatusCodes.Status200OK);
            Assert.True(envelope.Sucesso);
            Assert.Equal(mensagem, envelope.Mensagem);
        }

        public static T Criado<T>(IActionResult resultado, int id, string mensagem)
        {
            var criado = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal("GetById", criado.ActionName);
            Assert.Equal(id, criado.RouteValues!["id"]);

            var envelope = Assert.IsType<ApiResponse<T>>(criado.Value);
            Assert.True(envelope.Sucesso);
            Assert.Equal(mensagem, envelope.Mensagem);
            return envelope.Dados!;
        }

        public static void Invalido(IActionResult resultado, string mensagem = "Dados inválidos.") =>
            Falha(resultado, StatusCodes.Status400BadRequest, mensagem, [Validacao.Erro]);

        public static void NaoEncontrado(IActionResult resultado, string mensagem) =>
            Falha(resultado, StatusCodes.Status404NotFound, mensagem);

        public static void Falha(IActionResult resultado, int status, string mensagem, string[]? erros = null)
        {
            var envelope = Envelope<object>(resultado, status);
            Assert.False(envelope.Sucesso);
            Assert.Equal(mensagem, envelope.Mensagem);
            if (erros is not null)
                Assert.Equal(erros, envelope.Erros);
        }
    }
}
