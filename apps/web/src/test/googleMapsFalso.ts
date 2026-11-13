import type { PontoSelecionado } from '../lib/googleMaps'

/**
 * Substituto de `lib/googleMaps` para os testes — o SDK do Google não roda em jsdom. Os
 * componentes falsos (`CampoEndereco`, `MapaRota`) estão em `mapaFalso.tsx`. Uso:
 *
 *   vi.mock('../lib/googleMaps', () => import('../test/googleMapsFalso'))
 *   vi.mock('../components/CampoEndereco', () => import('../test/mapaFalso'))
 *   vi.mock('../components/MapaRota', () => import('../test/mapaFalso'))
 *
 * Sem chave (`mapaDisponivel: false`) o `TrajetoRota` dispensa o `APIProvider`, mas o resto dele
 * — cálculo pelo `POST /rota/calcular`, zerar o traçado ao trocar de ponto — roda de verdade.
 */
export const CHAVE_GOOGLE_MAPS = ''
export const mapaDisponivel = false
export const BIBLIOTECAS_MAPS: string[] = []
export const CENTRO_PADRAO = { lat: -15.78, lng: -47.93 }

/** O que a "sugestão do Google" devolve para cada campo. */
export const PONTOS_FALSOS: Record<string, PontoSelecionado> = {
  enderecoPartida: { endereco: 'Rua A, 100 — Joinville/SC', latitude: -26.304408, longitude: -48.848671 },
  enderecoChegada: { endereco: 'Rua B, 200 — Curitiba/PR', latitude: -25.428954, longitude: -49.267137 },
}
