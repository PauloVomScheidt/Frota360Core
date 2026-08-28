namespace Frota360.Application.DTOs.Usuario.Request
{
    /// <summary>
    /// Edição do próprio cadastro (LGPD, Art. 18, III — direito de correção). O id do usuário
    /// vem do claim <c>sub</c>, nunca do corpo: não há como corrigir o dado pessoal de outra
    /// pessoa por aqui.
    ///
    /// O e-mail fica de fora de propósito — é a chave de login e mexeria em convite, refresh
    /// token e no índice único global de <c>Usuario.Email</c>.
    /// </summary>
    public class AtualizarPerfilRequest
    {
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Opcionais. Só os 11 dígitos do CPF — a máscara é coisa da interface. Em branco
        /// grava nulo, e não string vazia: o índice único filtrado <c>(EmpresaId, CPF)</c>
        /// depende disso para deixar de fora quem não informou.
        /// </summary>
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }
    }
}
