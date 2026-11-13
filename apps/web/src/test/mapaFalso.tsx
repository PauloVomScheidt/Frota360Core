import type { PontoSelecionado } from '../lib/googleMaps'
import { PONTOS_FALSOS } from './googleMapsFalso'

/** `CampoEndereco` sem Places: um input comum e um botão que simula escolher a sugestão. */
export function CampoEndereco({
  id,
  label,
  valor,
  disabled,
  onTextoChange,
  onSelecionar,
}: {
  id: string
  label: string
  valor: string
  disabled?: boolean
  onTextoChange: (texto: string) => void
  onSelecionar: (ponto: PontoSelecionado) => void
}) {
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input id={id} required disabled={disabled} value={valor} onChange={(e) => onTextoChange(e.target.value)} />
      <button type="button" onClick={() => onSelecionar(PONTOS_FALSOS[id])}>
        Escolher sugestão para {label}
      </button>
    </div>
  )
}

/** `MapaRota` sem mapa: só diz se há traçado, e qual. */
export function MapaRota({ polyline }: { polyline: string | null }) {
  return <div role="img" aria-label={polyline ? `Mapa com o traçado ${polyline}` : 'Mapa sem traçado'} />
}
