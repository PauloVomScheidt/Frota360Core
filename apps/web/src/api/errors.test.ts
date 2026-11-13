import { http as msw, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { envelopeDeErro } from '../test/fabricas'
import { API, servidor } from '../test/servidor'
import { mensagensDeErro } from './errors'
import { ApiError, http } from './http'

async function falhaDe(promessa: Promise<unknown>): Promise<unknown> {
  try {
    await promessa
  } catch (erro) {
    return erro
  }
  throw new Error('a requisição deveria ter falhado')
}

function responderEm(caminho: string, resposta: () => Response) {
  servidor.use(msw.get(`${API}${caminho}`, resposta))
  return falhaDe(http.get(caminho))
}

describe('mensagensDeErro', () => {
  it('ApiError mostra os erros de validação, um por linha', () => {
    expect(mensagensDeErro(new ApiError('Falha', ['Placa obrigatória.', 'Km inválido.']))).toEqual([
      'Placa obrigatória.',
      'Km inválido.',
    ])
  })

  it('ApiError sem erros mostra a mensagem', () => {
    expect(mensagensDeErro(new ApiError('Veículo não encontrado.', []))).toEqual(['Veículo não encontrado.'])
  })

  it('422 com envelope mostra os erros de regra de negócio', async () => {
    const erro = await responderEm('/veiculo', () =>
      HttpResponse.json(envelopeDeErro(['A placa já está cadastrada.']), { status: 422 }),
    )
    expect(mensagensDeErro(erro)).toEqual(['A placa já está cadastrada.'])
  })

  it.each([403, 404, 429])('%s com envelope sem lista de erros mostra a mensagem', async (status) => {
    const erro = await responderEm('/veiculo', () =>
      HttpResponse.json({ sucesso: false, mensagem: 'Mensagem do servidor.', dados: null, erros: null }, { status }),
    )
    expect(mensagensDeErro(erro)).toEqual(['Mensagem do servidor.'])
  })

  it('envelope com lista de erros vazia mostra a mensagem', async () => {
    const erro = await responderEm('/veiculo', () =>
      HttpResponse.json(envelopeDeErro([], 'Acesso negado.'), { status: 403 }),
    )
    expect(mensagensDeErro(erro)).toEqual(['Acesso negado.'])
  })

  it('API fora do ar é dita com todas as letras', async () => {
    const erro = await responderEm('/veiculo', () => HttpResponse.error())
    expect(mensagensDeErro(erro)).toEqual(['Não foi possível conectar à API. Verifique se ela está no ar.'])
  })

  it('resposta sem envelope (ex.: 500 de proxy) cai na mensagem genérica', async () => {
    const erro = await responderEm('/veiculo', () => new HttpResponse('<html>Bad Gateway</html>', { status: 502 }))
    expect(mensagensDeErro(erro)).toEqual(['Ocorreu um erro. Tente novamente.'])
  })

  it('a tela pode trocar a mensagem genérica', () => {
    expect(mensagensDeErro(new Error('qualquer'), 'Não foi possível entrar.')).toEqual(['Não foi possível entrar.'])
  })

  it.each([new Error('x'), 'texto', null, undefined])('qualquer outra falha vira a mensagem genérica (%s)', (erro) => {
    expect(mensagensDeErro(erro)).toEqual(['Ocorreu um erro. Tente novamente.'])
  })
})
