namespace Frota360.Application.DTOs.Backoffice.Response
{
    public class EmpresaProvisionadaResponse
    {
        public int EmpresaId { get; set; }
        public string NomeEmpresa { get; set; } = string.Empty;
        public string EmailAdmin { get; set; } = string.Empty;
        public string LinkConvite { get; set; } = string.Empty;
    }
}
