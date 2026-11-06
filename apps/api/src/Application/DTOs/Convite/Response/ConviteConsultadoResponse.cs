namespace Frota360.Application.DTOs.Convite.Response
{
    /// <summary>
    /// O que a tela de aceite mostra antes da criação da conta. Só o que o dono do token já
    /// sabe ou precisa saber — nunca id de empresa, quem convidou ou o hash.
    /// </summary>
    public class ConviteConsultadoResponse
    {
        public string NomeEmpresa { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
    }
}
