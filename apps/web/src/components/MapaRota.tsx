import { useEffect } from 'react'
import { AdvancedMarker, Map, Pin, useMap, useMapsLibrary } from '@vis.gl/react-google-maps'
import { CENTRO_PADRAO } from '../lib/googleMaps'

interface Ponto {
  lat: number
  lng: number
}

/**
 * Mapa do trajeto: marcador de partida, de chegada e a linha entre eles.
 *
 * A polyline é desenhada de forma imperativa porque o `@vis.gl/react-google-maps` não
 * traz um componente para ela — só para marcadores e para o mapa.
 */
export function MapaRota({
  partida,
  chegada,
  polyline,
}: {
  partida: Ponto | null
  chegada: Ponto | null
  polyline: string | null
}) {
  return (
    <Map
      className="mapa-rota"
      defaultCenter={CENTRO_PADRAO}
      defaultZoom={3}
      gestureHandling="cooperative"
      disableDefaultUI
      zoomControl
      mapId="frota360-rota"
    >
      {partida && (
        <AdvancedMarker position={partida} title="Partida">
          <Pin background="#1f3a5f" borderColor="#1f3a5f" glyphColor="#fdfaf6" />
        </AdvancedMarker>
      )}
      {chegada && (
        <AdvancedMarker position={chegada} title="Chegada">
          <Pin background="#a03123" borderColor="#a03123" glyphColor="#fdfaf6" />
        </AdvancedMarker>
      )}
      <Tracado partida={partida} chegada={chegada} polyline={polyline} />
    </Map>
  )
}

function Tracado({
  partida,
  chegada,
  polyline,
}: {
  partida: Ponto | null
  chegada: Ponto | null
  polyline: string | null
}) {
  const map = useMap()
  const geometry = useMapsLibrary('geometry')

  useEffect(() => {
    if (!map) return

    // `decodePath` vem da biblioteca `geometry` do próprio Maps — decodificar a polyline
    // não precisa de dependência extra.
    const caminho =
      polyline && geometry ? geometry.encoding.decodePath(polyline) : null

    const linha = caminho
      ? new google.maps.Polyline({
          path: caminho,
          map,
          strokeColor: '#1f3a5f',
          strokeOpacity: 0.9,
          strokeWeight: 4,
        })
      : null

    // Enquadra o que existir: o traçado inteiro quando há, ou os dois pontos soltos
    // enquanto o cálculo não voltou.
    const limites = new google.maps.LatLngBounds()
    if (caminho) caminho.forEach((p) => limites.extend(p))
    else {
      if (partida) limites.extend(partida)
      if (chegada) limites.extend(chegada)
    }

    if (!limites.isEmpty()) {
      map.fitBounds(limites, 32)
      // Um ponto só não tem área: o fitBounds levaria ao zoom máximo.
      if (!caminho && !(partida && chegada)) map.setZoom(13)
    }

    return () => linha?.setMap(null)
  }, [map, geometry, polyline, partida, chegada])

  return null
}
