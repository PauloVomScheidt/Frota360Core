import { http, HttpResponse } from 'msw'
import { envelope } from './fabricas'
import { API, servidor } from './servidor'

type Metodo = 'get' | 'post' | 'put' | 'delete'

export interface Chamada {
  url: URL
  params: Record<string, string>
  corpo: Record<string, unknown> | undefined
}

/**
 * Registra um handler e devolve a lista das chamadas que ele recebeu — com os parâmetros da
 * query e o corpo já lidos —, para o teste conferir o que a tela mandou ao servidor.
 */
export function capturar(
  metodo: Metodo,
  caminho: string,
  responder: (chamada: Chamada) => Response | Promise<Response> = () => HttpResponse.json(envelope(null)),
): Chamada[] {
  const chamadas: Chamada[] = []
  servidor.use(
    http[metodo](`${API}${caminho}`, async ({ request }) => {
      const url = new URL(request.url)
      const texto = await request.text()
      const chamada: Chamada = {
        url,
        params: Object.fromEntries(url.searchParams),
        corpo: texto ? JSON.parse(texto) : undefined,
      }
      chamadas.push(chamada)
      return responder(chamada)
    }),
  )
  return chamadas
}

export const responderJson = (dados: unknown) => () => HttpResponse.json(envelope(dados))

export const responderErro = (status: number, erros: string[], mensagem = 'Falha na operação.') => () =>
  HttpResponse.json({ sucesso: false, mensagem, dados: null, erros }, { status })
