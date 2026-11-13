namespace Frota360.Application.DTOs.TipoCombustivel.Request
{
    public class UpdateTipoCombustivelRequest
    {
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
    }
}
