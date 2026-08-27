namespace Frota360.Application.DTOs.Convite.Response
{
    /// <summary>Retornado apenas na criação: inclui o link em claro para o admin encaminhar manualmente se preciso.</summary>
    public class ConviteCriadoResponse : ConviteResponse
    {
        public string LinkConvite { get; set; } = string.Empty;
    }
}
