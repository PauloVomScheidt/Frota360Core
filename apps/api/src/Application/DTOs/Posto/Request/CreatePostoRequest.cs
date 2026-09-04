namespace Frota360.Application.DTOs.Posto.Request
{
    public class CreatePostoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Cnpj { get; set; }
        public string? Cidade { get; set; }
    }
}
