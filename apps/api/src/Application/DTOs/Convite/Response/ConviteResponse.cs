namespace Frota360.Application.DTOs.Convite.Response
{
    public class ConviteResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime? UtilizadoEm { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
