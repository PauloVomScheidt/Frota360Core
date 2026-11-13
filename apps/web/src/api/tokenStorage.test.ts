import { describe, expect, it } from 'vitest'
import { fabricarSessao } from '../test/fabricas'
import { tokenStorage, USER_KEY } from './tokenStorage'

describe('tokenStorage', () => {
  it('a chave tem versão, para um formato antigo sumir em vez de quebrar', () => {
    expect(USER_KEY).toBe('frota360.user:v1')
  })

  it('guarda só a identidade exibida na UI: nome, e-mail, papel e empresa', () => {
    tokenStorage.setSession(fabricarSessao({ nome: 'Ana', email: 'ana@x.com', role: 'Supervisor', nomeEmpresa: 'Frota X' }))

    expect(JSON.parse(localStorage.getItem(USER_KEY)!)).toEqual({
      nome: 'Ana',
      email: 'ana@x.com',
      role: 'Supervisor',
      nomeEmpresa: 'Frota X',
    })
  })

  it('nunca grava token no localStorage, mesmo que a resposta traga um', () => {
    const sessaoComToken = { ...fabricarSessao(), token: 'jwt-secreto', refreshToken: 'refresh-secreto' }
    tokenStorage.setSession(sessaoComToken)

    const gravado = localStorage.getItem(USER_KEY)!
    expect(gravado).not.toContain('jwt-secreto')
    expect(gravado).not.toContain('refresh-secreto')
  })

  it('lê a identidade gravada', () => {
    tokenStorage.setSession(fabricarSessao({ nome: 'Ana', role: 'Motorista' }))
    expect(tokenStorage.getUser()).toMatchObject({ nome: 'Ana', role: 'Motorista' })
  })

  it('sem sessão não há usuário', () => {
    expect(tokenStorage.getUser()).toBeNull()
  })

  it('sessão corrompida é tratada como deslogado, sem quebrar', () => {
    localStorage.setItem(USER_KEY, '{nao-e-json')
    expect(tokenStorage.getUser()).toBeNull()
  })

  it('ignora a chave sem versão de um formato antigo', () => {
    localStorage.setItem('frota360.user', JSON.stringify({ nome: 'Antigo', role: 'Admin' }))
    expect(tokenStorage.getUser()).toBeNull()
  })

  it('atualizarNome troca só o nome, depois de editar o perfil', () => {
    tokenStorage.setSession(fabricarSessao({ nome: 'Ana', email: 'ana@x.com', role: 'Operador', nomeEmpresa: 'Frota X' }))
    tokenStorage.atualizarNome('Ana Souza')

    expect(tokenStorage.getUser()).toEqual({
      nome: 'Ana Souza',
      email: 'ana@x.com',
      role: 'Operador',
      nomeEmpresa: 'Frota X',
    })
  })

  it('atualizarNome sem sessão não cria uma', () => {
    tokenStorage.atualizarNome('Ninguém')
    expect(tokenStorage.getUser()).toBeNull()
  })

  it('clear encerra a sessão', () => {
    tokenStorage.setSession(fabricarSessao())
    tokenStorage.clear()
    expect(tokenStorage.getUser()).toBeNull()
  })
})
