using FluentValidation;

namespace Frota360.Application.Common
{
    /// <summary>
    /// Todo request de listagem paginada. Existir como interface é o que permite validar a
    /// paginação num lugar só, em vez de repetir as duas regras (e o teto) em cada validator.
    /// </summary>
    public interface IRequestPaginado
    {
        int Pagina { get; set; }
        int TamanhoPagina { get; set; }
    }

    public static class RegrasDePaginacao
    {
        /// <summary>
        /// Teto do que a API entrega de uma vez. Sem ele, um <c>tamanhoPagina=999999</c>
        /// materializa o histórico inteiro da empresa em memória — que é exatamente o que a
        /// paginação veio impedir.
        /// </summary>
        public const int TamanhoMaximoPagina = 100;

        /// <summary>
        /// Chame no construtor do validator da listagem. As mensagens vão para o usuário final,
        /// como toda mensagem de validação do projeto.
        /// </summary>
        public static void AplicarRegrasDePaginacao<T>(this AbstractValidator<T> validator)
            where T : IRequestPaginado
        {
            validator.RuleFor(x => x.Pagina)
                .GreaterThan(0).WithMessage("Página deve ser maior que zero.");

            validator.RuleFor(x => x.TamanhoPagina)
                .InclusiveBetween(1, TamanhoMaximoPagina)
                .WithMessage($"Tamanho da página deve ficar entre 1 e {TamanhoMaximoPagina}.");
        }
    }
}
