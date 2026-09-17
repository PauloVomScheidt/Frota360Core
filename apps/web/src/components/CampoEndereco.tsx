import { useEffect, useRef, useState } from 'react'
import { useMapsLibrary } from '@vis.gl/react-google-maps'
import type { PontoSelecionado } from '../lib/googleMaps'

/**
 * Campo de endereço com autocomplete do Places API (New).
 *
 * O `PlaceAutocompleteElement` é um Web Component, não um componente React: ele é
 * instanciado à mão e anexado a um contêiner. Foi escolhido no lugar do widget legado
 * justamente por causa do `FormDialog`: o `Autocomplete` antigo injeta o dropdown no
 * `<body>`, que fica atrás do top layer de um `<dialog>` modal. Este renderiza a lista
 * dentro de si, então funciona dentro do diálogo.
 *
 * Sem chave ou com a Places fora do ar, o campo vira um `<input>` de texto comum: o
 * endereço é obrigatório na API, a coordenada não — dá para cadastrar a rota do mesmo
 * jeito, só sem traçado.
 */
export function CampoEndereco({
  id,
  label,
  valor,
  onTextoChange,
  onSelecionar,
  onIndisponivel,
  autoFocus,
  disabled,
}: {
  id: string
  label: string
  valor: string
  onTextoChange: (texto: string) => void
  onSelecionar: (ponto: PontoSelecionado) => void
  onIndisponivel: (mensagem: string) => void
  autoFocus?: boolean
  disabled?: boolean
}) {
  const places = useMapsLibrary('places')
  const container = useRef<HTMLDivElement>(null)
  const elemento = useRef<google.maps.places.PlaceAutocompleteElement | null>(null)
  const [montado, setMontado] = useState(false)

  // Refs para os callbacks: o listener do Web Component é registrado uma vez só, e sem
  // isto ele capturaria o `form` da primeira renderização. A atualização vai num efeito
  // sem lista de dependências — escrever em ref durante o render é o que a regra
  // `react(refs)` proíbe.
  const aoSelecionar = useRef(onSelecionar)
  const aoIndisponivel = useRef(onIndisponivel)
  const aoDigitar = useRef(onTextoChange)

  useEffect(() => {
    aoSelecionar.current = onSelecionar
    aoIndisponivel.current = onIndisponivel
    aoDigitar.current = onTextoChange
  })

  useEffect(() => {
    if (!places || !container.current) return

    const autocomplete = new places.PlaceAutocompleteElement({
      // Endereço brasileiro: o viés evita sugerir a cidade homônima de outro país.
      includedRegionCodes: ['br'],
    })
    autocomplete.id = id
    autocomplete.className = 'input campo-endereco'

    // ⚠️ O evento entrega `placePrediction`, NÃO `place`: a seleção é uma predição, e vira
    // um Place por `toPlace()`. Isso também é o que faz o `fetchFields` seguinte herdar o
    // session token do autocomplete, que é o que mantém a busca na sessão cobrada como uma
    // só. O listener é registrado pela sobrecarga tipada do elemento (sem `as`), para o
    // compilador conferir o tipo do evento em vez de acreditar numa afirmação nossa.
    async function selecionou(evento: google.maps.places.PlacePredictionSelectEvent) {
      try {
        const local = evento.placePrediction.toPlace()
        await local.fetchFields({ fields: ['formattedAddress', 'location'] })

        const endereco = local.formattedAddress ?? ''
        const coordenada = local.location

        if (!endereco || !coordenada) {
          aoIndisponivel.current('O endereço escolhido não trouxe coordenada.')
          if (endereco) aoDigitar.current(endereco)
          return
        }

        aoSelecionar.current({
          endereco,
          latitude: coordenada.lat(),
          longitude: coordenada.lng(),
        })
      } catch (erro) {
        // Sem o log a falha some: o usuário vê o aviso, e quem for depurar não tem pista.
        console.error('Falha ao obter os detalhes do endereço no Places.', erro)
        aoIndisponivel.current('Não foi possível obter os detalhes do endereço.')
      }
    }

    // A lista de sugestões é desenhada DENTRO do elemento, e o formulário vive num
    // `.dialog-corpo` com `overflow: auto` — que a recorta. Trazer o campo para o topo da
    // área rolável ao focar dá à lista a altura de que ela precisa: numa janela de 620px o
    // espaço abaixo do campo passa de 290px para 347px, e a lista (~300px, atribuição do
    // Google inclusive) deixa de nascer cortada. Onde não há rolagem, é no-op.
    function aproximarDoTopo() {
      autocomplete.scrollIntoView({ block: 'start' })
    }

    autocomplete.addEventListener('focus', aproximarDoTopo)
    autocomplete.addEventListener('gmp-select', selecionou)
    container.current.appendChild(autocomplete)
    elemento.current = autocomplete
    setMontado(true)

    // `autoFocus` do React não alcança o Web Component: o atributo vale para o <input> do
    // fallback, e quando o Places carrega quem está na tela é o elemento do Google. Sem
    // isto o diálogo abre com o foco no body — o formulário perdia o foco inicial que
    // tinha antes do autocomplete.
    //
    // ⚠️ O foco vai num quadro seguinte: chamado logo após o appendChild, o elemento ainda
    // não montou o campo interno e o focus() é engolido em silêncio.
    const quadro = autoFocus ? requestAnimationFrame(() => autocomplete.focus()) : null

    return () => {
      if (quadro !== null) cancelAnimationFrame(quadro)
      autocomplete.removeEventListener('focus', aproximarDoTopo)
      autocomplete.removeEventListener('gmp-select', selecionou)
      autocomplete.remove()
      elemento.current = null
      setMontado(false)
    }
  }, [places, id, autoFocus])

  // O valor vem do estado do formulário (inclusive ao abrir a edição de uma rota já
  // salva); o Web Component não é controlado pelo React, então é sincronizado à mão.
  useEffect(() => {
    if (elemento.current && elemento.current.value !== valor) elemento.current.value = valor
  }, [valor, montado])

  useEffect(() => {
    if (elemento.current) elemento.current.disabled = disabled ?? false
  }, [disabled, montado])

  return (
    <div className="field campo-largo">
      <label htmlFor={id}>{label}</label>
      {places ? (
        <div ref={container} />
      ) : (
        <input
          id={id}
          className="input"
          type="text"
          placeholder={label}
          required
          autoFocus={autoFocus}
          disabled={disabled}
          value={valor}
          onChange={(e) => onTextoChange(e.target.value)}
        />
      )}
    </div>
  )
}
