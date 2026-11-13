import { describe, expect, it } from 'vitest'
import { validarSenha } from './senha'

const MINIMO = 'A senha deve ter no mínimo 6 caracteres.'
const MAIUSCULA = 'A senha deve conter ao menos 1 letra maiúscula.'
const NUMERO = 'A senha deve conter ao menos 1 número.'
const CONFIRMACAO = 'As senhas não conferem.'

describe('validarSenha — regras da API: 6 caracteres, 1 maiúscula, 1 número', () => {
  it('aceita a senha mínima que cumpre as três regras', () => {
    expect(validarSenha('Senha1')).toEqual([])
  })

  it.each([
    ['Sen1A', [MINIMO]],
    ['senha1', [MAIUSCULA]],
    ['Senhaa', [NUMERO]],
    ['', [MINIMO, MAIUSCULA, NUMERO]],
    ['abc', [MINIMO, MAIUSCULA, NUMERO]],
  ])('"%s" → %o', (senha, erros) => {
    expect(validarSenha(senha)).toEqual(erros)
  })

  it('maiúscula acentuada não conta como maiúscula', () => {
    expect(validarSenha('Ésenha1')).toEqual([MAIUSCULA])
  })
})

describe('validarSenha — confirmação', () => {
  it('sem confirmação, não compara', () => {
    expect(validarSenha('Senha1')).toEqual([])
  })

  it('confirmação igual passa', () => {
    expect(validarSenha('Senha1', 'Senha1')).toEqual([])
  })

  it('confirmação diferente acusa, junto com os demais erros', () => {
    expect(validarSenha('Senha1', 'Senha2')).toEqual([CONFIRMACAO])
    expect(validarSenha('abc', 'abd')).toEqual([MINIMO, MAIUSCULA, NUMERO, CONFIRMACAO])
  })

  it('confirmação vazia é diferente de uma senha preenchida', () => {
    expect(validarSenha('Senha1', '')).toEqual([CONFIRMACAO])
  })
})
