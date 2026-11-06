namespace Frota360.Application.DTOs.Usuario.Response
{
    /// <summary>
    /// O próprio cadastro mais a empresa a que ele pertence — só leitura: nome e CNPJ da
    /// empresa não são editáveis por ninguém do painel.
    /// </summary>
    public class PerfilResponse : UsuarioResponse
    {
        public string NomeEmpresa { get; set; } = string.Empty;
        public string? CnpjEmpresa { get; set; }
    }
}
