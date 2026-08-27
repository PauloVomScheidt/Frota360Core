namespace Frota360.Application.DTOs.Rota.Response
{
    public class RotaResponse
    {
        public int Id { get; set; }
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public int CodigoMotorista { get; set; }

        /// <summary>
        /// Nome do motorista no momento da leitura, desnormalizado como em
        /// <c>ManutencaoResponse</c>. É o que mantém a rota identificável depois que a
        /// pessoa muda de perfil e some da lista de motoristas.
        /// </summary>
        public string? NomeMotorista { get; set; }
        public int CodigoVeiculo { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public DateTime DataInclusao { get; set; }
        public int KmInicial { get; set; }
        public int? KmFinal { get; set; }
        public int? KmPercorrido { get; set; }
    }
}
