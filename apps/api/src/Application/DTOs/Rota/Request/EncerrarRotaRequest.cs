namespace Frota360.Application.DTOs.Rota.Request
{
    public class EncerrarRotaRequest
    {
        /// <summary>Odômetro do veículo no fim da rota.</summary>
        public int KmFinal { get; set; }

        /// <summary>Opcional — quando omitida, o encerramento é registrado como agora.</summary>
        public DateTime? DataFim { get; set; }
    }
}
