/** Regras de senha da API (§6.7): ≥ 6 caracteres, 1 maiúscula, 1 número. */
export function validarSenha(senha: string, confirmacao?: string): string[] {
  const erros: string[] = []
  if (senha.length < 6) erros.push('A senha deve ter no mínimo 6 caracteres.')
  if (!/[A-Z]/.test(senha)) erros.push('A senha deve conter ao menos 1 letra maiúscula.')
  if (!/\d/.test(senha)) erros.push('A senha deve conter ao menos 1 número.')
  if (confirmacao !== undefined && senha !== confirmacao) erros.push('As senhas não conferem.')
  return erros
}
