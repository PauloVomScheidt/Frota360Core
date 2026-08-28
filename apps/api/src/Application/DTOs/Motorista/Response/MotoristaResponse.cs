namespace Frota360.Application.DTOs.Motorista.Response
{
    /// <summary>
    /// Um motorista é um <c>Usuario</c> com a role Motorista — não há entidade própria.
    /// O <c>Id</c> é o id do usuário, e é ele que a rota grava em <c>CodigoMotorista</c>.
    /// </summary>
    public class MotoristaResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>Opcionais: só existem se a pessoa os informou ao aceitar o convite.</summary>
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }

        public bool Ativo { get; set; }
        public DateTime DataInclusao { get; set; }
    }
}
