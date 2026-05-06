namespace Frota360.Application.Validators
{
    internal static class ValidatorHelpers
    {

        public static bool Is18YearsOld(DateTime dataNascimento)
            => dataNascimento <= DateTime.Today.AddYears(-18);

        public static bool IsValidCPF(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
                return false;

            if (cpf.Distinct().Count() == 1)
                return false;

            var soma = 0;
            for (var i = 0; i < 9; i++)
                soma += int.Parse(cpf[i].ToString()) * (10 - i);

            var resto = soma % 11;
            var digito1 = resto < 2 ? 0 : 11 - resto;

            if (int.Parse(cpf[9].ToString()) != digito1)
                return false;

            soma = 0;
            for (var i = 0; i < 10; i++)
                soma += int.Parse(cpf[i].ToString()) * (11 - i);

            resto = soma % 11;
            var digito2 = resto < 2 ? 0 : 11 - resto;

            return int.Parse(cpf[10].ToString()) == digito2;

        }
    }
}