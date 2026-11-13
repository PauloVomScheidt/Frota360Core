using Frota360.Domain.Common;
using System.Net;
using System.Text.Json;

namespace Frota360.Api.Middlewares
{
    public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Erro não tratado. Método: {Metodo} | Rota: {Rota} | Mensagem: {Mensagem}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, mensagem) = ex switch
            {
                ArgumentNullException => (HttpStatusCode.BadRequest, "Requisição inválida."),
                InvalidOperationException => (HttpStatusCode.UnprocessableEntity, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Não autorizado."),
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente mais tarde.")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse<object>.Fail(mensagem);
            var json = JsonSerializer.Serialize(response, OpcoesJson);

            await context.Response.WriteAsync(json, context.RequestAborted);
        }
    }
}
