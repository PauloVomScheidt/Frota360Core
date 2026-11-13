namespace Frota360.Application.DTOs.Posto.Request
{
    public class UpdatePostoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Cnpj { get; set; }
        public string? Cidade { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
