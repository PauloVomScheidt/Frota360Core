import { delay, http as msw, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { envelope, envelopeDeErro, fabricarSessao, fabricarVeiculo } from '../test/fabricas'
import { API, servidor } from '../test/servidor'
import { abastecimentosApi } from './abastecimentos'
import { ApiError, http, unwrap } from './http'
import { tokenStorage } from './tokenStorage'
import type { ApiResponse } from './types'

const naoAutorizado = () => HttpResponse.json(envelopeDeErro([], 'Sessão expirada.'), { status: 401 })

describe('configuração', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
    vi.resetModules()
  })

  it('sem VITE_API_URL o app quebra no carregamento, em vez de chamar a origem do front', async () => {
    vi.stubEnv('VITE_API_URL', '')
    vi.resetModules()
    await expect(import('./http')).rejects.toThrow('VITE_API_URL não definida')
  })
})

describe('unwrap', () => {
  it('devolve os dados de uma resposta de sucesso', () => {
    const veiculo = fabricarVeiculo()
    expect(unwrap(envelope(veiculo))).toEqual(veiculo)
  })

  it('sucesso=false vira ApiError com a mensagem e os erros do envelope', () => {
    const resposta = envelopeDeErro(['Placa obrigatória.', 'Km inválido.'], 'Dados inválidos.')

    expect(() => unwrap(resposta)).toThrow(ApiError)
    try {
      unwrap(resposta)
    } catch (erro) {
      expect(erro).toBeInstanceOf(Error)
      expect(erro).toMatchObject({
        name: 'ApiError',
        message: 'Dados inválidos.',
        erros: ['Placa obrigatória.', 'Km inválido.'],
      })
    }
  })

  it('envelope de erro sem lista de erros vira ApiError com lista vazia', () => {
    const resposta: ApiResponse<null> = { sucesso: false, mensagem: 'Não encontrado.', dados: null, erros: null }
    expect(() => unwrap(resposta)).toThrow(expect.objectContaining({ message: 'Não encontrado.', erros: [] }))
  })

  it('sucesso com dados nulos devolve null — caso de GET /abastecimento/anterior sem histórico', async () => {
    servidor.use(msw.get(`${API}/abastecimento/anterior`, () => HttpResponse.json(envelope(null))))
    await expect(abastecimentosApi.anterior(1, 50_000)).resolves.toBeNull()
  })

  it('quem decide a falha é o sucesso: sucesso=false lança mesmo trazendo dados', () => {
    const resposta: ApiResponse<number> = { sucesso: false, mensagem: 'Recusado.', dados: 42, erros: null }
    expect(() => unwrap(resposta)).toThrow(ApiError)
  })
})

describe('requisições autenticadas', () => {
  it('usa a URL da API configurada em VITE_API_URL', async () => {
    let urlRecebida = ''
    servidor.use(
      msw.get(`${API}/veiculo`, ({ request }) => {
        urlRecebida = request.url
        return HttpResponse.json(envelope([]))
      }),
    )
    await http.get('/veiculo')
    expect(urlRecebida).toBe(`${import.meta.env.VITE_API_URL}/veiculo`)
  })

  it('não envia header Authorization: o token viaja só no cookie HttpOnly', async () => {
    tokenStorage.setSession(fabricarSessao({ role: 'Admin' }))
    let authorization: string | null = 'não lido'
    servidor.use(
      msw.get(`${API}/veiculo`, ({ request }) => {
        authorization = request.headers.get('authorization')
        return HttpResponse.json(envelope([]))
      }),
    )
    await http.get('/veiculo')
    expect(authorization).toBeNull()
  })

  it('envia credenciais, para o navegador anexar o cookie numa chamada a outra origem', async () => {
    let credenciais = ''
    servidor.use(
      msw.get(`${API}/veiculo`, ({ request }) => {
        credenciais = request.credentials
        return HttpResponse.json(envelope([]))
      }),
    )
    await http.get('/veiculo')
    expect(credenciais).toBe('include')
  })
})

describe('sessão expirada (401)', () => {
  const assign = vi.fn()
  let chamadasDeRefresh = 0
  let corpoDoRefresh: string | null = null

  function refreshComSucesso(sessao = fabricarSessao({ role: 'Supervisor' })) {
    return msw.post(`${API}/auth/refresh`, async ({ request }) => {
      chamadasDeRefresh++
      corpoDoRefresh = await request.text()
      await delay(20)
      return HttpResponse.json(envelope(sessao))
    })
  }

  /** 401 até o refresh acontecer, depois 200 — o que o servidor faz quando o cookie é renovado. */
  function expiraAteRenovar(caminho: string, dados: unknown) {
    return msw.get(`${API}${caminho}`, () => (chamadasDeRefresh > 0 ? HttpResponse.json(envelope(dados)) : naoAutorizado()))
  }

  beforeEach(() => {
    chamadasDeRefresh = 0
    corpoDoRefresh = null
    assign.mockReset()
    vi.stubGlobal('location', { ...window.location, assign })
    tokenStorage.setSession(fabricarSessao({ role: 'Operador' }))
  })

  afterEach(() => vi.unstubAllGlobals())

  it('renova a sessão e repete a requisição original, sem o usuário perceber', async () => {
    const veiculos = [fabricarVeiculo()]
    servidor.use(refreshComSucesso(), expiraAteRenovar('/veiculo', veiculos))

    const { data } = await http.get('/veiculo')

    expect(unwrap(data)).toEqual(veiculos)
    expect(chamadasDeRefresh).toBe(1)
    expect(assign).not.toHaveBeenCalled()
  })

  it('o refresh não leva corpo: o refresh token vai no cookie', async () => {
    servidor.use(refreshComSucesso(), expiraAteRenovar('/veiculo', []))
    await http.get('/veiculo')
    expect(corpoDoRefresh === '' || corpoDoRefresh === '{}').toBe(true)
  })

  it('o refresh atualiza a identidade guardada: mudança de papel passa a valer', async () => {
    servidor.use(
      refreshComSucesso(fabricarSessao({ nome: 'Ana', role: 'Supervisor', nomeEmpresa: 'Frota X' })),
      expiraAteRenovar('/veiculo', []),
    )
    await http.get('/veiculo')
    expect(tokenStorage.getUser()).toMatchObject({ nome: 'Ana', role: 'Supervisor', nomeEmpresa: 'Frota X' })
  })

  it('várias requisições expiradas ao mesmo tempo disparam um único refresh', async () => {
    servidor.use(refreshComSucesso(), expiraAteRenovar('/veiculo', ['v']), expiraAteRenovar('/posto', ['p']))

    const [veiculos, postos] = await Promise.all([http.get('/veiculo'), http.get('/posto')])

    expect(chamadasDeRefresh).toBe(1)
    expect(unwrap(veiculos.data)).toEqual(['v'])
    expect(unwrap(postos.data)).toEqual(['p'])
  })

  it('refresh recusado encerra a sessão e manda para o login', async () => {
    servidor.use(
      msw.get(`${API}/veiculo`, naoAutorizado),
      msw.post(`${API}/auth/refresh`, () => {
        chamadasDeRefresh++
        return naoAutorizado()
      }),
    )

    await expect(http.get('/veiculo')).rejects.toMatchObject({ response: { status: 401 } })
    expect(chamadasDeRefresh).toBe(1)
    expect(tokenStorage.getUser()).toBeNull()
    expect(assign).toHaveBeenCalledWith('/login')
  })

  it('refresh que responde sucesso sem sessão também encerra a sessão', async () => {
    servidor.use(
      msw.get(`${API}/veiculo`, naoAutorizado),
      msw.post(`${API}/auth/refresh`, () => HttpResponse.json(envelope(null))),
    )

    await expect(http.get('/veiculo')).rejects.toBeDefined()
    expect(tokenStorage.getUser()).toBeNull()
    expect(assign).toHaveBeenCalledWith('/login')
  })

  it('repete a requisição só uma vez: 401 de novo depois do refresh encerra a sessão', async () => {
    servidor.use(refreshComSucesso(), msw.get(`${API}/veiculo`, naoAutorizado))

    await expect(http.get('/veiculo')).rejects.toMatchObject({ response: { status: 401 } })
    expect(chamadasDeRefresh).toBe(1)
    expect(tokenStorage.getUser()).toBeNull()
    expect(assign).toHaveBeenCalledWith('/login')
  })

  it.each([
    '/auth/login',
    '/auth/refresh',
    '/auth/esqueci-senha',
    '/auth/redefinir-senha',
    '/convite/consultar',
    '/convite/aceitar',
  ])('401 em %s é credencial inválida, não sessão expirada: sem refresh e sem logout', async (caminho) => {
    servidor.use(msw.post(`${API}${caminho}`, naoAutorizado), refreshComSucesso())

    await expect(http.post(caminho, {})).rejects.toMatchObject({ response: { status: 401 } })
    expect(chamadasDeRefresh).toBe(0)
    expect(tokenStorage.getUser()).not.toBeNull()
    expect(assign).not.toHaveBeenCalled()
  })

  it.each([403, 404, 422, 500])('%s não tenta refresh e chega à tela como erro', async (status) => {
    servidor.use(
      msw.get(`${API}/veiculo`, () => HttpResponse.json(envelopeDeErro(['falha']), { status })),
      refreshComSucesso(),
    )

    await expect(http.get('/veiculo')).rejects.toMatchObject({ response: { status } })
    expect(chamadasDeRefresh).toBe(0)
    expect(tokenStorage.getUser()).not.toBeNull()
  })

  it('falha de rede não tenta refresh nem derruba a sessão', async () => {
    servidor.use(msw.get(`${API}/veiculo`, () => HttpResponse.error()), refreshComSucesso())

    await expect(http.get('/veiculo')).rejects.toBeDefined()
    expect(chamadasDeRefresh).toBe(0)
    expect(tokenStorage.getUser()).not.toBeNull()
  })
})
