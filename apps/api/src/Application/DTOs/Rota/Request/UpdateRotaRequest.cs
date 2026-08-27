namespace Frota360.Application.DTOs.Rota.Request
{
    /// <summary>
    /// Edição dos dados de planejamento da rota. Ativo e DataFim não entram aqui:
    /// encerrar é a única transição de estado, e passa por POST /rota/{id}/encerrar.
    /// </summary>
    public class UpdateRotaRequest
    {
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public int CodigoMotorista { get; set; }
        public int CodigoVeiculo { get; set; }
        public DateTime DataInicio { get; set; }
    }
}
