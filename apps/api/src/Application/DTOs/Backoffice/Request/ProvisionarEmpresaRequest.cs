namespace Frota360.Application.DTOs.Backoffice.Request
{
    public class ProvisionarEmpresaRequest
    {
        public string NomeEmpresa { get; set; } = string.Empty;
        public string? CNPJ { get; set; }
        public string EmailAdmin { get; set; } = string.Empty;
    }
}
