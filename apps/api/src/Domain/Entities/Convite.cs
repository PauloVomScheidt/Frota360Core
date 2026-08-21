namespace Frota360.Domain.Entities
{
    public class Convite
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime? UtilizadoEm { get; set; }

        /// <summary>Null quando o convite foi gerado pelo backoffice (provisionamento de empresa).</summary>
        public int? CriadoPorUsuarioId { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
