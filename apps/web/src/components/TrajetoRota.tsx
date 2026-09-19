import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { APIProvider } from '@vis.gl/react-google-maps'
import { CampoEndereco } from './CampoEndereco'
import { MapaRota } from './MapaRota'
import { rotasApi } from '../api/rotas'
import { mensagensDeErro } from '../api/errors'
import { formatDistancia, formatDuracao } from '../lib/format'
import {
  BIBLIOTECAS_MAPS,
  CHAVE_GOOGLE_MAPS,
  mapaDisponivel,
  type PontoSelecionado,
} from '../lib/googleMaps'

/** Ponto trocado invalida o trajeto: distância e traçado antigos não valem mais. */
const SEM_TRACADO = {
  distanciaMetros: null,
  duracaoEstimadaSegundos: null,
  polylineCodificada: null,
}

export interface TrajetoForm {
  enderecoPartida: string
  enderecoChegada: string
  latitudePartida: number | null
  longitudePartida: number | null
  latitudeChegada: number | null
  longitudeChegada: number | null
  distanciaMetros: number | null
  duracaoEstimadaSegundos: number | null
  polylineCodificada: string | null
}

/**
 * Bloco "Trajeto" do formulário de rota, compartilhado por `/rotas` e `/minhas-rotas` —
 * os dois pediam exatamente os mesmos campos.
 *
 * O `APIProvider` vive aqui, e não na raiz do app, para o script do Maps só ser baixado
 * por quem abre o formulário de rota. Como só um diálogo fica aberto por vez, nunca há
 * duas instâncias montadas.
 */
export function TrajetoRota(props: {
  form: TrajetoForm
  onFormChange: (form: TrajetoForm) => void
  autoFocus?: boolean
  disabled?: boolean
}) {
  const [falhouAoCarregar, setFalhouAoCarregar] = useState(false)

  // Sem chave o campo já nasce como texto puro, sem aviso: em produção a chave pode
  // simplesmente não ter sido provisionada, e alarmar todo usuário por isso seria ruído.
  // Chave presente que falha ao carregar é outra história — aí houve um erro de verdade
  // (chave inválida, referenciador bloqueado, rede) e vale dizer.
  if (!mapaDisponivel || falhouAoCarregar) {
    return (
      <CamposDeTrajeto
        {...props}
        avisoDeCarregamento={
          falhouAoCarregar
            ? 'O Google Maps não pôde ser carregado, então não há autocomplete nem traçado.'
            : null
        }
      />
    )
  }

  return (
    <APIProvider
      apiKey={CHAVE_GOOGLE_MAPS}
      libraries={BIBLIOTECAS_MAPS}
      language="pt-BR"
      region="BR"
      onError={() => setFalhouAoCarregar(true)}
    >
      <CamposDeTrajeto {...props} avisoDeCarregamento={null} />
    </APIProvider>
  )
}

function CamposDeTrajeto({
  form,
  onFormChange,
  autoFocus,
  disabled,
  avisoDeCarregamento,
}: {
  form: TrajetoForm
  onFormChange: (form: TrajetoForm) => void
  autoFocus?: boolean
  disabled?: boolean
  avisoDeCarregamento: string | null
}) {
  // Texto digitado sem escolher da lista não tem coordenada: zerá-la evita salvar um
  // endereço com o ponto do endereço anterior.
  function digitou(campo: 'partida' | 'chegada', texto: string) {
    onFormChange({
      ...form,
      ...SEM_TRACADO,
      ...(campo === 'partida'
        ? { enderecoPartida: texto, latitudePartida: null, longitudePartida: null }
        : { enderecoChegada: texto, latitudeChegada: null, longitudeChegada: null }),
    })
  }

  function selecionou(campo: 'partida' | 'chegada', ponto: PontoSelecionado) {
    setAvisoEndereco(null)
    onFormChange({
      ...form,
      ...SEM_TRACADO,
      ...(campo === 'partida'
        ? {
            enderecoPartida: ponto.endereco,
            latitudePartida: ponto.latitude,
            longitudePartida: ponto.longitude,
          }
        : {
            enderecoChegada: ponto.endereco,
            latitudeChegada: ponto.latitude,
            longitudeChegada: ponto.longitude,
          }),
    })
  }

  const [avisoEndereco, setAvisoEndereco] = useState<string | null>(null)

  const partida =
    form.latitudePartida != null && form.longitudePartida != null
      ? { lat: form.latitudePartida, lng: form.longitudePartida }
      : null

  const chegada =
    form.latitudeChegada != null && form.longitudeChegada != null
      ? { lat: form.latitudeChegada, lng: form.longitudeChegada }
      : null

  // Traçado que já veio do banco (edição de rota calculada antes). Trocar qualquer ponto
  // o zera via SEM_TRACADO, e aí o cálculo volta a valer.
  const tracadoSalvo =
    form.distanciaMetros != null && form.duracaoEstimadaSegundos != null
      ? {
          distanciaMetros: form.distanciaMetros,
          duracaoEstimadaSegundos: form.duracaoEstimadaSegundos,
          polylineCodificada: form.polylineCodificada,
        }
      : null

  // ⚠️ Cada cálculo é uma chamada COBRADA pela Google, e duas coisas aqui existem só por
  // causa disso:
  //
  // 1. `enabled` exige que NÃO haja traçado salvo. Sem essa condição, abrir uma rota já
  //    calculada para editar pagaria de novo pelo mesmo trajeto.
  // 2. A chave arredonda a coordenada em 6 casas, que é a precisão da coluna
  //    `numeric(9,6)`. O Places devolve precisão cheia; sem o arredondamento, o ponto
  //    lido do banco e o mesmo ponto escolhido na tela geram chaves diferentes — e o
  //    cache erra justamente onde ele mais importa.
  const chave = (n: number) => Math.round(n * 1e6) / 1e6

  const calculo = useQuery({
    queryKey: partida && chegada
      ? ['rota', 'calculo', chave(partida.lat), chave(partida.lng), chave(chegada.lat), chave(chegada.lng)]
      : ['rota', 'calculo', null],
    queryFn: () =>
      rotasApi.calcular({
        latitudeOrigem: partida!.lat,
        longitudeOrigem: partida!.lng,
        latitudeDestino: chegada!.lat,
        longitudeDestino: chegada!.lng,
      }),
    enabled: partida !== null && chegada !== null && tracadoSalvo === null,
    staleTime: Infinity,
    retry: false,
  })

  const trajeto = tracadoSalvo ?? calculo.data

  // O 422 da API já vem com texto de usuário ("Não foi possível calcular o trajeto
  // agora..."), que cobre indisponibilidade, timeout e ausência de trajeto entre os
  // pontos — o motivo técnico fica no log do servidor. `mensagensDeErro` também cobre
  // a API fora do ar.
  const mensagemDeAviso =
    avisoDeCarregamento ??
    (calculo.error
      ? mensagensDeErro(calculo.error, 'Não foi possível calcular o trajeto.')[0]
      : avisoEndereco)

  useEffect(() => {
    if (!trajeto) return
    if (
      form.distanciaMetros === trajeto.distanciaMetros &&
      form.duracaoEstimadaSegundos === trajeto.duracaoEstimadaSegundos
    ) {
      return
    }
    onFormChange({
      ...form,
      distanciaMetros: trajeto.distanciaMetros,
      duracaoEstimadaSegundos: trajeto.duracaoEstimadaSegundos,
      polylineCodificada: trajeto.polylineCodificada ?? null,
    })
  }, [trajeto, form, onFormChange])

  return (
    <>
      <CampoEndereco
        id="enderecoPartida"
        label="Partida"
        valor={form.enderecoPartida}
        autoFocus={autoFocus}
        disabled={disabled}
        onTextoChange={(texto) => digitou('partida', texto)}
        onSelecionar={(ponto) => selecionou('partida', ponto)}
        onIndisponivel={setAvisoEndereco}
      />
      <CampoEndereco
        id="enderecoChegada"
        label="Chegada"
        valor={form.enderecoChegada}
        disabled={disabled}
        onTextoChange={(texto) => digitou('chegada', texto)}
        onSelecionar={(ponto) => selecionou('chegada', ponto)}
        onIndisponivel={setAvisoEndereco}
      />

      {(partida || chegada) && (
        <div className="campo-largo">
          <MapaRota
            partida={partida}
            chegada={chegada}
            polyline={trajeto?.polylineCodificada ?? null}
          />
          {calculo.isFetching && <p className="trajeto-resumo trajeto-nota">Calculando o trajeto…</p>}
          {trajeto && (
            <p className="trajeto-resumo">
              <strong>{formatDistancia(trajeto.distanciaMetros)}</strong>
              <span aria-hidden="true"> · </span>
              <strong>{formatDuracao(trajeto.duracaoEstimadaSegundos)}</strong>
              <span className="trajeto-nota"> estimados pelo Google</span>
            </p>
          )}
        </div>
      )}

      {mensagemDeAviso && (
        <p className="campo-largo trajeto-aviso" role="status">
          {mensagemDeAviso} A rota pode ser cadastrada assim — o trajeto é opcional.
        </p>
      )}
    </>
  )
}
