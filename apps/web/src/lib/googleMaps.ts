/**
 * Chave de browser do Maps JavaScript + Places API (New). O Vite a embute no bundle em
 * tempo de build: ela é pública por construção, e quem a protege é a restrição por
 * referenciador HTTP no console do Google — nunca o segredo dela.
 *
 * Vazia é estado válido: sem chave o formulário de rota cai para campo de texto puro,
 * e a rota continua sendo cadastrada só com o endereço (coordenada é opcional na API).
 */
export const CHAVE_GOOGLE_MAPS = import.meta.env.VITE_GOOGLE_MAPS_KEY ?? ''

export const mapaDisponivel = CHAVE_GOOGLE_MAPS !== ''

/** Bibliotecas carregadas pelo APIProvider: autocomplete, marcador e decodePath. */
export const BIBLIOTECAS_MAPS = ['places', 'marker', 'geometry']

/** Centro do mapa antes de haver trajeto: Brasil inteiro à vista. */
export const CENTRO_PADRAO = { lat: -15.78, lng: -47.93 }

export interface PontoSelecionado {
  endereco: string
  latitude: number
  longitude: number
}
