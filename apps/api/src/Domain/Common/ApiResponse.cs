namespace Frota360.Domain.Common
{
    public class ApiResponse<T>
    {
        public bool Sucesso { get; set; }
        public string? Mensagem { get; set; }
        public T? Dados { get; set; }
        public IEnumerable<string>? Erros { get; set; }

        public static ApiResponse<T> Ok(T dados, string mensagem = "Operação realizada com sucesso.")
            => new() { Sucesso = true, Mensagem = mensagem, Dados = dados };

        public static ApiResponse<T> Fail(string mensagem, IEnumerable<string>? erros = null)
            => new() { Sucesso = false, Mensagem = mensagem, Erros = erros };
    }
}
