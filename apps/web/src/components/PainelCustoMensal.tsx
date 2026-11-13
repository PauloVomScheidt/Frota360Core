import { useState } from 'react'
import type { CustoPorMesResponse } from '../api/types'
import { formatMoeda, formatMoedaCompacta } from '../lib/format'
import { escalaDeTresMarcas, nomeDoMes, percentual, rotuloDoMes, SERIES_CUSTO, type SerieCusto } from '../lib/custo'
import type { Intervalo } from '../lib/periodo'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const ALTURA_PLOT = 280
/** A escala ocupa 240 dos 280 px: a folga acima é onde cabe o total da coluna mais alta. */
const ALTURA_ESCALA = 240
const ALTURA_MINIMA_SEGMENTO = 4
/** Sólida, e não translúcida: o total da coluna selecionada usa a mesma cor de fundo para cobrir a linha de média. */
const FUNDO_SELECIONADO = 'color-mix(in srgb, var(--color-accent) 7%, var(--color-bg))'

/** Mês em andamento: comparado dia a dia com o mesmo trecho do mês anterior, não com ele inteiro. */
export interface ComparacaoParcial {
  total: number
  atual: Intervalo
  anterior: Intervalo
}

interface Comparacao {
  variacao: number | null
  rotulo: string
  nota: string | null
}

function diaMes(iso: string): string {
  const [, mes, dia] = iso.split('-')
  return `${Number(dia)}/${mes}`
}

function trecho(intervalo: Intervalo): string {
  return `1 a ${diaMes(intervalo.ate)}`
}

function compararMes(meses: CustoPorMesResponse[], indice: number, parcial: ComparacaoParcial | undefined): Comparacao {
  const atual = meses[indice].total
  if (atual <= 0)
    return {
      variacao: null,
      rotulo: '',
      nota: 'Nenhum custo lançado neste mês.',
    }

  if (parcial && indice === meses.length - 1) {
    const rotulo = `vs. ${trecho(parcial.anterior)}`
    const nota = `Mês em andamento: compara ${trecho(parcial.atual)} com ${trecho(parcial.anterior)}.`
    if (parcial.total <= 0) {
      return {
        variacao: null,
        rotulo: '',
        nota: `Sem registros de ${trecho(parcial.anterior)} para comparar.`,
      }
    }
    return {
      variacao: ((atual - parcial.total) / parcial.total) * 100,
      rotulo,
      nota,
    }
  }

  if (indice === 0) return { variacao: null, rotulo: '', nota: 'Primeiro mês do período.' }

  const anterior = meses[indice - 1]
  if (anterior.total <= 0) {
    return {
      variacao: null,
      rotulo: '',
      nota: `Sem registros em ${rotuloDoMes(anterior.ano, anterior.mes)} para comparar.`,
    }
  }

  return {
    variacao: ((atual - anterior.total) / anterior.total) * 100,
    rotulo: `vs. ${rotuloDoMes(anterior.ano, anterior.mes)}`,
    nota: null,
  }
}

/**
 * Posicionado sobre o gráfico inteiro, não sobre a coluna: `fracao` é o centro da coluna (0–1),
 * e o mesmo valor no `left` e no `translateX` faz o tooltip deslizar para dentro nas bordas —
 * na primeira coluna ele encosta à esquerda, na última à direita, e nunca sai do gráfico.
 */
function TooltipCustoMensal({
  mes,
  serie,
  fracao,
  base,
}: {
  mes: CustoPorMesResponse
  serie: SerieCusto
  fracao: number
  base: number
}) {
  const ativa = SERIES_CUSTO.find((s) => s.chave === serie)!
  const valor = ativa.valor(mes)

  return (
    <div
      role="tooltip"
      className="pointer-events-none absolute z-10 w-[230px] max-w-full p-3"
      style={{
        left: `${fracao * 100}%`,
        transform: `translateX(-${fracao * 100}%)`,
        bottom: base,
        border: '1px solid var(--color-divider)',
        background: 'var(--color-bg)',
      }}
    >
      <div className="text-[11px] uppercase" style={{ letterSpacing: '0.08em', color: mutedText }}>
        {nomeDoMes(mes)}
      </div>
      <div className="mt-1 text-lg leading-tight font-extrabold" style={{ fontFamily: 'var(--font-heading)' }}>
        {formatMoeda(valor)}
      </div>
      <div className="mt-0.5 flex items-center gap-1.5 text-xs">
        <span style={{ width: 12, height: 2, background: ativa.cor }} />
        {ativa.rotulo} · {percentual(valor, mes.total)} do mês
      </div>
      <div className="mt-2 flex flex-col gap-1 pt-2" style={{ borderTop: '1px solid var(--color-divider)' }}>
        {SERIES_CUSTO.map((s) => (
          <div
            key={s.chave}
            className="flex items-center justify-between gap-2 text-xs"
            style={{
              color: s.chave === serie ? 'var(--color-text)' : mutedText,
              fontWeight: s.chave === serie ? 600 : 400,
            }}
          >
            <span className="flex items-center gap-1.5">
              <span style={{ width: 12, height: 2, background: s.cor }} />
              {s.rotulo}
            </span>
            <span className="tabular-nums">{formatMoeda(s.valor(mes))}</span>
          </div>
        ))}
        <div className="flex items-center justify-between gap-2 text-xs font-semibold">
          <span>Total do mês</span>
          <span className="tabular-nums">{formatMoeda(mes.total)}</span>
        </div>
      </div>
    </div>
  )
}

/** Custo subir pede atenção (âmbar); cair é o desejável (verde). */
function ChipDeVariacao({ variacao }: { variacao: number }) {
  const arredondada = Math.round(variacao * 10) / 10
  const [cor, fundo] =
    arredondada > 0
      ? ['var(--color-warning)', 'var(--color-warning-bg)']
      : arredondada < 0
        ? ['var(--color-success)', 'var(--color-success-bg)']
        : ['var(--color-neutral-700)', 'var(--color-neutral-200)']
  const sinal = arredondada > 0 ? '+' : arredondada < 0 ? '−' : ''

  return (
    <span className="rounded-full px-2 py-1 font-bold tabular-nums" style={{ color: cor, background: fundo }}>
      {sinal}
      {Math.abs(arredondada).toLocaleString('pt-BR', {
        maximumFractionDigits: 1,
      })}
      %
    </span>
  )
}

function ResumoDoMes({ mes, comparacao }: { mes: CustoPorMesResponse; comparacao: Comparacao }) {
  return (
    <aside
      className="flex flex-col gap-4 border-b pb-6 lg:border-r lg:border-b-0 lg:pr-10 lg:pb-0"
      style={{ borderColor: 'var(--color-neutral-200)' }}
      aria-live="polite"
    >
      <div className="text-xs font-semibold uppercase" style={{ letterSpacing: '0.08em', color: mutedText }}>
        {nomeDoMes(mes)}
      </div>
      <div className="text-[32px] leading-none font-extrabold sm:text-[40px]" style={{ letterSpacing: '-0.02em' }}>
        {formatMoeda(mes.total)}
      </div>

      {comparacao.variacao !== null ? (
        <div className="flex flex-col gap-1.5">
          <div className="flex items-center gap-2 text-[13px]">
            <ChipDeVariacao variacao={comparacao.variacao} />
            <span style={{ color: mutedText }}>{comparacao.rotulo}</span>
          </div>
          {comparacao.nota && (
            <p className="m-0 text-xs" style={{ color: mutedText }}>
              {comparacao.nota}
            </p>
          )}
        </div>
      ) : (
        <p className="m-0 text-[13px]" style={{ color: mutedText }}>
          {comparacao.nota}
        </p>
      )}

      <div
        className="mt-1 flex h-2.5 gap-[2px] overflow-hidden rounded-full"
        style={{ background: 'var(--color-neutral-200)' }}
      >
        {mes.total > 0 &&
          SERIES_CUSTO.filter((s) => s.valor(mes) > 0).map((s) => (
            <div
              key={s.chave}
              style={{
                flexBasis: 0,
                flexGrow: s.valor(mes),
                background: s.cor,
              }}
            />
          ))}
      </div>

      <div className="flex flex-col gap-2.5">
        {SERIES_CUSTO.map((s) => (
          <div
            key={s.chave}
            className="grid grid-cols-[10px_1fr_auto_36px] items-center gap-2 text-[13px] sm:gap-2.5 sm:text-sm"
          >
            <span className="h-2.5 w-2.5 rounded-[3px]" style={{ background: s.cor }} />
            <span>{s.rotulo}</span>
            <span className="font-semibold tabular-nums">{formatMoeda(s.valor(mes))}</span>
            <span className="text-right text-[13px] tabular-nums" style={{ color: mutedText }}>
              {mes.total > 0 ? percentual(s.valor(mes), mes.total) : '—'}
            </span>
          </div>
        ))}
      </div>
    </aside>
  )
}

/**
 * Resumo do mês selecionado à esquerda, colunas empilhadas à direita. Passar o mouse, clicar ou
 * focar uma coluna seleciona o mês; passar o mouse numa fatia abre o tooltip dela — as duas
 * leituras convivem. Começa no último mês com lançamento.
 *
 * Espera os meses já completados com zero (`preencherMeses`): mês vazio vira "sem registros".
 */
export function PainelCustoMensal({
  titulo,
  meses,
  comparacaoParcial,
  textoVazio,
  atualizando = false,
}: {
  titulo: string
  meses: CustoPorMesResponse[]
  comparacaoParcial?: ComparacaoParcial
  textoVazio: string
  atualizando?: boolean
}) {
  const ultimoComDados = meses.reduce((ultimo, m, i) => (m.total > 0 ? i : ultimo), -1)
  const [selecionado, setSelecionado] = useState<number | null>(null)
  const [fatia, setFatia] = useState<{ mes: number; serie: SerieCusto } | null>(null)

  const indice = selecionado ?? Math.max(ultimoComDados, 0)
  const maximo = Math.max(0, ...meses.map((m) => m.total))
  const { teto, marcas } = escalaDeTresMarcas(maximo)
  const alturaDe = (valor: number) => (valor / teto) * ALTURA_ESCALA
  const alturaSegmento = (valor: number) => Math.max(alturaDe(valor), ALTURA_MINIMA_SEGMENTO)

  const comDados = meses.filter((m) => m.total > 0)
  const media = comDados.length >= 2 ? comDados.reduce((soma, m) => soma + m.total, 0) / comDados.length : null

  function alturaDaPilha(mes: CustoPorMesResponse): number {
    const valores = SERIES_CUSTO.map((s) => s.valor(mes)).filter((v) => v > 0)
    return valores.reduce((soma, v) => soma + alturaSegmento(v), 0) + 2 * Math.max(valores.length - 1, 0)
  }

  return (
    <section
      className="mb-8 flex flex-col gap-6 rounded-[14px] px-4 py-5 sm:px-8 sm:py-7"
      style={{
        border: '1px solid var(--color-neutral-300)',
        background: 'var(--color-bg)',
        opacity: atualizando ? 0.6 : 1,
        transition: 'opacity 150ms',
      }}
    >
      <header className="flex flex-wrap items-end justify-between gap-x-6 gap-y-2">
        <div>
          <h3 style={{ margin: '0 0 4px' }}>{titulo}</h3>
          <p className="m-0 text-sm" style={{ color: mutedText }}>
            Combustível, manutenção e despesas lançados por mês
          </p>
        </div>
        {maximo > 0 && (
          <div className="flex flex-wrap items-center gap-x-5 gap-y-1 text-[13px]" style={{ color: mutedText }}>
            {media !== null && (
              <span className="flex items-center gap-2">
                <span className="w-4" style={{ borderTop: '1.5px dashed var(--color-neutral-500)' }} />
                Média{' '}
                <strong className="font-semibold tabular-nums" style={{ color: 'var(--color-text)' }}>
                  {formatMoedaCompacta(media)}
                </strong>
              </span>
            )}
            <span>Passe o mouse ou toque em um mês para ver o detalhe</span>
          </div>
        )}
      </header>

      {maximo <= 0 ? (
        <p className="m-0 text-[13px]" style={{ color: mutedText }}>
          {textoVazio}
        </p>
      ) : (
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-[300px_minmax(0,1fr)]">
          <ResumoDoMes mes={meses[indice]} comparacao={compararMes(meses, indice, comparacaoParcial)} />

          <div className="flex items-start gap-2 sm:gap-3">
            <div className="relative w-[48px] flex-none sm:w-[60px]" style={{ height: ALTURA_PLOT }} aria-hidden="true">
              {marcas.map((marca) => (
                <span
                  key={marca}
                  className="absolute right-0 text-[11px] whitespace-nowrap tabular-nums sm:text-xs"
                  style={{ bottom: alturaDe(marca) - 7, color: mutedText }}
                >
                  {formatMoedaCompacta(marca)}
                </span>
              ))}
            </div>

            <div className="flex min-w-0 flex-1 flex-col gap-2.5">
              <div className="relative" style={{ height: ALTURA_PLOT }} onMouseLeave={() => setFatia(null)}>
                {marcas.map((marca) => (
                  <div
                    key={marca}
                    className="absolute right-0 left-0"
                    style={{
                      bottom: alturaDe(marca),
                      borderTop: '1px solid var(--color-neutral-200)',
                    }}
                  />
                ))}

                <div className="absolute inset-0 grid grid-cols-6 gap-1 sm:gap-3">
                  {meses.map((mes, i) => {
                    const ativo = i === indice
                    const temDados = mes.total > 0
                    return (
                      <button
                        key={`${mes.ano}-${mes.mes}`}
                        type="button"
                        aria-label={`${nomeDoMes(mes)}: ${temDados ? formatMoeda(mes.total) : 'sem registros'}`}
                        aria-pressed={ativo}
                        className="flex h-full min-w-0 cursor-pointer flex-col items-center justify-end rounded-t-[10px] border-0 p-0"
                        style={{
                          background: ativo ? FUNDO_SELECIONADO : 'transparent',
                        }}
                        onMouseEnter={() => setSelecionado(i)}
                        onClick={() => setSelecionado(i)}
                        onFocus={() => setSelecionado(i)}
                      >
                        {temDados ? (
                          <>
                            <span
                              className="relative z-[2] mb-1.5 hidden rounded px-1 text-xs font-bold whitespace-nowrap tabular-nums sm:inline"
                              style={{
                                color: ativo ? 'var(--color-text)' : mutedText,
                                background: ativo ? FUNDO_SELECIONADO : 'var(--color-bg)',
                              }}
                            >
                              {formatMoedaCompacta(mes.total)}
                            </span>
                            <span
                              className="flex flex-col-reverse gap-[2px] overflow-hidden rounded-t-md"
                              style={{ width: 'min(56px, 80%)' }}
                            >
                              {SERIES_CUSTO.filter((s) => s.valor(mes) > 0).map((s) => (
                                <span
                                  key={s.chave}
                                  className="block flex-none"
                                  style={{
                                    height: alturaSegmento(s.valor(mes)),
                                    background: s.cor,
                                    opacity: fatia === null || (fatia.mes === i && fatia.serie === s.chave) ? 1 : 0.35,
                                    transition: 'opacity 150ms',
                                  }}
                                  onMouseEnter={() => setFatia({ mes: i, serie: s.chave })}
                                />
                              ))}
                            </span>
                          </>
                        ) : (
                          <>
                            <span className="mb-2.5 hidden text-xs sm:inline" style={{ color: mutedText }}>
                              sem registros
                            </span>
                            <span
                              className="block"
                              style={{
                                width: 'min(56px, 80%)',
                                borderTop: '2px dashed var(--color-neutral-300)',
                              }}
                            />
                          </>
                        )}
                      </button>
                    )
                  })}
                </div>

                {media !== null && (
                  <div
                    className="pointer-events-none absolute right-0 left-0 z-[1]"
                    style={{
                      bottom: alturaDe(media),
                      borderTop: '1.5px dashed var(--color-neutral-500)',
                    }}
                  />
                )}

                <div
                  className="pointer-events-none absolute right-0 bottom-0 left-0"
                  style={{ borderTop: '1px solid var(--color-neutral-400)' }}
                />

                {fatia && (
                  <TooltipCustoMensal
                    mes={meses[fatia.mes]}
                    serie={fatia.serie}
                    fracao={(fatia.mes + 0.5) / meses.length}
                    base={alturaDaPilha(meses[fatia.mes]) + 30}
                  />
                )}
              </div>

              <div className="grid grid-cols-6 gap-1 sm:gap-3" aria-hidden="true">
                {meses.map((mes, i) => (
                  <span
                    key={`${mes.ano}-${mes.mes}`}
                    className="text-center text-[11px] sm:text-[13px]"
                    style={{
                      color: i === indice ? 'var(--color-text)' : mutedText,
                      fontWeight: i === indice ? 700 : 500,
                    }}
                  >
                    <span className="hidden sm:inline">{rotuloDoMes(mes.ano, mes.mes)}</span>
                    <span className="sm:hidden">{rotuloDoMes(mes.ano, mes.mes).slice(0, 3)}</span>
                  </span>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
