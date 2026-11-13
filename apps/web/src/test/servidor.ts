import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import {
  envelope,
  fabricarMotorista,
  fabricarPosto,
  fabricarTipoCombustivel,
  fabricarTipoDespesa,
  fabricarTipoManutencao,
  fabricarVeiculo,
} from './fabricas'

export const API = import.meta.env.VITE_API_URL

export const catalogoPadrao = {
  veiculos: [
    fabricarVeiculo({ id: 1, nomeVeiculo: 'Caminhão 1', placa: 'ABC1D01' }),
    fabricarVeiculo({ id: 2, nomeVeiculo: 'Caminhão 2', placa: 'ABC1D02', quilometragem: 120_000 }),
  ],
  motoristas: [
    fabricarMotorista({ id: 10, nome: 'João Pereira' }),
    fabricarMotorista({ id: 11, nome: 'Carla Souza' }),
  ],
  tiposCombustivel: [
    fabricarTipoCombustivel({ id: 1, nome: 'Diesel S10' }),
    fabricarTipoCombustivel({ id: 2, nome: 'Arla 32' }),
    fabricarTipoCombustivel({ id: 3, nome: 'Gasolina', ativo: false }),
  ],
  postos: [
    fabricarPosto({ id: 1, nome: 'Posto Central' }),
    fabricarPosto({ id: 2, nome: 'Posto Rodovia', ativo: false }),
  ],
  tiposManutencao: [
    fabricarTipoManutencao({ id: 1, nome: 'Troca de óleo' }),
    fabricarTipoManutencao({ id: 2, nome: 'Revisão de freios', intervaloKm: 30_000 }),
  ],
  tiposDespesa: [
    fabricarTipoDespesa({ id: 1, nome: 'Pedágio' }),
    fabricarTipoDespesa({ id: 2, nome: 'IPVA' }),
  ],
}

function listarCatalogo<T extends { ativo: boolean }>(itens: T[]) {
  return ({ request }: { request: Request }) => {
    const apenasAtivos = new URL(request.url).searchParams.get('apenasAtivos') === 'true'
    return HttpResponse.json(envelope(apenasAtivos ? itens.filter((i) => i.ativo) : itens))
  }
}

export const handlersPadrao = [
  http.get(`${API}/veiculo`, () => HttpResponse.json(envelope(catalogoPadrao.veiculos))),
  http.get(`${API}/motorista`, () => HttpResponse.json(envelope(catalogoPadrao.motoristas))),
  http.get(`${API}/tipocombustivel`, listarCatalogo(catalogoPadrao.tiposCombustivel)),
  http.get(`${API}/posto`, listarCatalogo(catalogoPadrao.postos)),
  http.get(`${API}/tipomanutencao`, listarCatalogo(catalogoPadrao.tiposManutencao)),
  http.get(`${API}/tipodespesa`, listarCatalogo(catalogoPadrao.tiposDespesa)),
]

export const servidor = setupServer(...handlersPadrao)
