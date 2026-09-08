import { useEffect, useRef, type FormEvent, type ReactNode } from 'react'
import { mensagensDeErro } from '../api/errors'
import { ErrorList } from './AppLayout'
import { PencilIcon, TrashIcon } from './icons'
import { PERIODOS, type Periodo } from '../lib/periodo'
import { TAMANHOS_PAGINA, type TamanhoPagina } from '../lib/paginacao'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

/**
 * Abre o `<dialog>` nativo assim que ele é montado (todo diálogo da aplicação já nasce
 * montado só quando deve aparecer — `{condicao && <ConfirmDialog ... />}`). `showModal()`
 * dá de graça o que antes era escrito à mão: trava de foco (Tab não escapa mais para
 * trás do diálogo), Escape para fechar e o backdrop.
 */
function useAbrirModalAoMontar(ref: React.RefObject<HTMLDialogElement | null>) {
  useEffect(() => {
    const dialogo = ref.current
    if (!dialogo) return

    dialogo.showModal()

    // ⚠️ Quando o formulário é longo o bastante para `.dialog-corpo` rolar, o Chrome torna
    // o próprio contêiner de rolagem focável (é ele quem responde a PageUp/PageDown) e o
    // `showModal()` pousa o foco ali, e não no primeiro campo: o anel de `:focus-visible`
    // contorna o formulário inteiro e quem abre o diálogo começa sem cursor em lugar nenhum.
    // A correção só age nesse caso exato — com o foco em qualquer outro lugar (o Cancelar
    // do ConfirmDialog, um `autoFocus` declarado na tela), nada acontece aqui.
    const corpo = dialogo.querySelector('.dialog-corpo')
    if (corpo === null || document.activeElement !== corpo) return

    dialogo
      .querySelector<HTMLElement>('input:not([tabindex="-1"]), select, textarea')
      ?.focus()
  }, [ref])
}

/** Linha de carregando / erro / vazio dentro de um tbody. */
export function TableStates({
  colSpan,
  pending,
  error,
  empty,
  textoCarregando = 'Carregando…',
  textoErro,
  textoVazio,
}: {
  colSpan: number
  pending: boolean
  error: unknown
  empty: boolean
  textoCarregando?: string
  textoErro: string
  textoVazio: string
}) {
  if (pending) {
    return (
      <tr>
        <td colSpan={colSpan} style={{ color: mutedText }}>
          {textoCarregando}
        </td>
      </tr>
    )
  }

  if (error) {
    return (
      <tr>
        <td colSpan={colSpan} style={{ color: 'var(--color-danger)' }}>
          {mensagensDeErro(error, textoErro)[0]}
        </td>
      </tr>
    )
  }

  if (empty) {
    return (
      <tr>
        <td colSpan={colSpan} style={{ color: mutedText }}>
          {textoVazio}
        </td>
      </tr>
    )
  }

  return null
}

/**
 * Botões de editar/excluir de uma linha. Cada ação só aparece quando a role do
 * usuário permite (§5 do CONTEXTO) — o handler ausente esconde o botão.
 */
export function RowActions({
  onEditar,
  onExcluir,
  descricao,
}: {
  onEditar?: () => void
  onExcluir?: () => void
  /** O que está sendo editado/excluído, para o rótulo acessível. Ex.: 'a rota Joinville → Curitiba'. */
  descricao: string
}) {
  return (
    <div className="flex justify-end gap-1">
      {onEditar && (
        <button
          type="button"
          className="btn btn-icon"
          style={{ borderRadius: 0 }}
          onClick={onEditar}
          title={`Editar ${descricao}`}
          aria-label={`Editar ${descricao}`}
        >
          <PencilIcon size={16} />
        </button>
      )}
      {onExcluir && (
        <button
          type="button"
          className="btn btn-icon btn-icon-danger"
          style={{ borderRadius: 0 }}
          onClick={onExcluir}
          title={`Excluir ${descricao}`}
          aria-label={`Excluir ${descricao}`}
        >
          <TrashIcon size={16} />
        </button>
      )}
    </div>
  )
}

/**
 * Confirmação de ação consequente — exclusão (irreversível) ou mudança de permissão
 * (derruba a sessão de outra pessoa). Nunca dispara direto do controle da linha.
 *
 * Os defaults são os da exclusão, que é o uso majoritário; `variante` existe porque
 * vermelho em "promover a Supervisor" alarmaria sem motivo.
 */
export function ConfirmDialog({
  titulo,
  mensagem,
  textoConfirmar = 'Excluir',
  textoPendente = 'Excluindo…',
  variante = 'perigo',
  pending,
  erros,
  onConfirmar,
  onCancelar,
}: {
  titulo: string
  mensagem: string
  textoConfirmar?: string
  textoPendente?: string
  variante?: 'perigo' | 'padrao'
  pending: boolean
  erros: string[]
  onConfirmar: () => void
  onCancelar: () => void
}) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  useAbrirModalAoMontar(dialogRef)

  return (
    <dialog
      ref={dialogRef}
      className="dialog"
      role="alertdialog"
      aria-labelledby="dialog-titulo"
      // O Escape do navegador fecha o <dialog> sozinho; isto só avisa o componente pai
      // (dono do estado "está aberto") para ele desmontar em vez de ficar dessincronizado.
      //
      // Sem clique-no-backdrop-fecha de propósito: um <dialog> não pode carregar um
      // segundo handler de clique sem ganhar um role interativo (button/link/...) que
      // brigaria com o role real dele (alertdialog) — daí o React Doctor apontar
      // no-noninteractive-element-interactions se ele existisse aqui. Cancelar continua
      // com dois caminhos totalmente acessíveis por teclado: Escape e o botão abaixo.
      onClose={onCancelar}
    >
      <h3 id="dialog-titulo" className="dialog-title">
        {titulo}
      </h3>
      <p className="dialog-body">{mensagem}</p>
      <ErrorList mensagens={erros} />
      <div className="dialog-actions">
        {/* O foco inicial fica em Cancelar: Enter reflexo não pode excluir. */}
        <button
          type="button"
          className="btn btn-secondary"
          style={{ borderRadius: 0, padding: '10px 18px' }}
          onClick={onCancelar}
          disabled={pending}
          autoFocus
        >
          Cancelar
        </button>
        <button
          type="button"
          className={variante === 'perigo' ? 'btn btn-danger' : 'btn btn-primary'}
          style={{ borderRadius: 0, padding: '10px 18px' }}
          onClick={onConfirmar}
          disabled={pending}
        >
          {pending ? textoPendente : textoConfirmar}
        </button>
      </div>
    </dialog>
  )
}

/**
 * Diálogo com formulário — todo cadastro/edição do painel e as transições de estado que
 * pedem campos (concluir manutenção, encerrar rota). Diferente do `ConfirmDialog`, que só
 * confirma uma ação destrutiva, aqui há campos a preencher.
 *
 * `largura` separa os dois usos: 520 (o default) para as transições, de dois ou três
 * campos; 760 para os formulários de cadastro, onde o `.dialog-grid` das seções tem espaço
 * para três colunas.
 */
export function FormDialog({
  titulo,
  descricao,
  textoConfirmar,
  textoPendente,
  largura = 520,
  pending,
  erros,
  onSubmit,
  onCancelar,
  children,
}: {
  titulo: string
  descricao?: ReactNode
  textoConfirmar: string
  textoPendente: string
  largura?: number
  pending: boolean
  erros: string[]
  onSubmit: (e: FormEvent) => void
  onCancelar: () => void
  children: ReactNode
}) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  useAbrirModalAoMontar(dialogRef)

  return (
    <dialog
      ref={dialogRef}
      className="dialog"
      aria-labelledby="form-dialog-titulo"
      // `calc(100vw - 2rem)` em vez de `100%`: no celular o diálogo encostaria nas duas
      // bordas da tela, e o backdrop deixa de se ver.
      style={{ width: `min(${largura}px, calc(100vw - 2rem))` }}
      // Mesmo padrão do ConfirmDialog logo acima — sem clique-no-backdrop, ver o
      // comentário lá para o motivo (no-noninteractive-element-interactions).
      onClose={onCancelar}
    >
      {/* display: contents — o form não pode ser o próprio <dialog> (só <dialog> ganha
          showModal/backdrop nativos), mas também não pode virar uma caixa própria: o
          layout em coluna com gap é do .dialog no pai, e os filhos precisam continuar
          participando dele como se estivessem soltos ali. */}
      <form onSubmit={onSubmit} style={{ display: 'contents' }}>
        <h3 id="form-dialog-titulo" className="dialog-title">
          {titulo}
        </h3>
        {descricao && <p className="dialog-body">{descricao}</p>}
        {/* Só os campos rolam: título e ações continuam à vista num formulário longo. */}
        <div className="dialog-corpo">{children}</div>
        <ErrorList mensagens={erros} />
        <div className="dialog-actions">
          <button
            type="button"
            className="btn btn-secondary"
            style={{ padding: '10px 18px' }}
            onClick={onCancelar}
            disabled={pending}
          >
            Cancelar
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ padding: '10px 18px' }}
            disabled={pending}
          >
            {pending ? textoPendente : textoConfirmar}
          </button>
        </div>
      </form>
    </dialog>
  )
}

/**
 * Bloco de campos dentro de um `FormDialog`. O título agrupa o que pertence junto ("Dados
 * do posto", "Veículo e motorista") — é o que impede um formulário de treze campos de virar
 * uma fileira sem hierarquia.
 *
 * Sem `titulo`, é só o grid: o formato dos diálogos curtos, que não têm o que separar.
 * A largura de cada campo é do grid, não do campo — quem precisa da linha inteira
 * (observação, aviso, nota) usa a classe `campo-largo`.
 */
export function SecaoCampos({ titulo, children }: { titulo?: string; children: ReactNode }) {
  return (
    <section>
      {titulo && <h4 className="dialog-secao-titulo">{titulo}</h4>}
      <div className="dialog-grid">{children}</div>
    </section>
  )
}

/**
 * O terceiro diálogo: só mostra conteúdo. O `ConfirmDialog` confirma uma ação e o
 * `FormDialog` submete campos — aqui não há o que decidir nem o que enviar, então a única
 * ação é fechar. Serve o detalhamento sob demanda, como os lançamentos de um veículo em
 * `/custos`.
 *
 * Herda dos outros dois o `<dialog>` nativo e o `useAbrirModalAoMontar` — trava de foco,
 * Escape, `::backdrop` e o reposicionamento de foco quando o corpo rola.
 */
export function PainelDialog({
  titulo,
  descricao,
  largura = 760,
  onFechar,
  children,
}: {
  titulo: string
  descricao?: ReactNode
  largura?: number
  onFechar: () => void
  children: ReactNode
}) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  useAbrirModalAoMontar(dialogRef)

  return (
    <dialog
      ref={dialogRef}
      className="dialog"
      aria-labelledby="painel-dialog-titulo"
      style={{ width: `min(${largura}px, calc(100vw - 2rem))` }}
      // Mesmo padrão dos outros dois — sem clique-no-backdrop, ver o comentário no
      // ConfirmDialog para o motivo.
      onClose={onFechar}
    >
      <h3 id="painel-dialog-titulo" className="dialog-title">
        {titulo}
      </h3>
      {descricao && <p className="dialog-body">{descricao}</p>}
      <div className="dialog-corpo">{children}</div>
      <div className="dialog-actions">
        <button
          type="button"
          className="btn btn-secondary"
          style={{ padding: '10px 18px' }}
          onClick={onFechar}
        >
          Fechar
        </button>
      </div>
    </dialog>
  )
}

/**
 * Rodapé de listagem paginada — o mesmo para quem pagina no cliente (a maioria, via
 * `usePaginacao`) e para as duas telas que paginam no servidor (`/auditoria`, `/custos`).
 *
 * ⚠️ **Some quando o total cabe na menor opção**, e não quando há uma página só. A regra
 * antiga (`totalPaginas <= 1`) escondia o seletor justamente de quem tem 12 registros e
 * queria ver 10 — e é o que permite ligar o rodapé em toda lista do painel sem encher um
 * catálogo de cinco linhas de ruído.
 */
export function Paginacao({
  pagina,
  totalPaginas,
  total,
  tamanhoPagina,
  onMudar,
  onMudarTamanho,
  pending,
}: {
  pagina: number
  totalPaginas: number
  total: number
  tamanhoPagina: number
  onMudar: (pagina: number) => void
  /** Ausente, o seletor não aparece — o rodapé fica só com a contagem e o passo a passo. */
  onMudarTamanho?: (tamanho: TamanhoPagina) => void
  pending?: boolean
}) {
  if (total <= TAMANHOS_PAGINA[0]) return null

  const primeiro = (pagina - 1) * tamanhoPagina + 1
  const ultimo = Math.min(pagina * tamanhoPagina, total)

  return (
    <div
      className="mt-4 flex flex-wrap items-center justify-between gap-4 py-3"
      style={{ borderTop: '1px solid var(--color-divider)' }}
    >
      <div className="flex items-center gap-3">
        {onMudarTamanho && (
          <label className="flex items-center gap-2 text-[13px]" style={{ color: mutedText }}>
            Itens por página
            <select
              className="input"
              style={{ width: 'auto', minHeight: 32, padding: '4px 8px' }}
              value={tamanhoPagina}
              onChange={(e) => onMudarTamanho(Number(e.target.value) as TamanhoPagina)}
            >
              {TAMANHOS_PAGINA.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </label>
        )}
        <span className="text-[13px]" style={{ color: mutedText }}>
          {primeiro}–{ultimo} de {total}
        </span>
      </div>
      <div className="flex items-center gap-2">
        <button
          type="button"
          className="btn btn-secondary"
          style={{ padding: '6px 14px' }}
          onClick={() => onMudar(pagina - 1)}
          disabled={pagina <= 1 || pending}
        >
          Anterior
        </button>
        <span className="text-[13px]" style={{ color: mutedText }}>
          {pagina} / {totalPaginas}
        </span>
        <button
          type="button"
          className="btn btn-secondary"
          style={{ padding: '6px 14px' }}
          onClick={() => onMudar(pagina + 1)}
          disabled={pagina >= totalPaginas || pending}
        >
          Próxima
        </button>
      </div>
    </div>
  )
}

/**
 * Filtro de período com opções prontas, no lugar de dois campos de data soltos.
 * Compartilhado por `/manutencoes` e `/abastecimentos` — dois filtros de data diferentes
 * no mesmo sistema seria a inconsistência que incomoda depois.
 */
export function FiltroPeriodo({
  valor,
  onMudar,
  rotulo = 'Período',
  id = 'filtroPeriodo',
}: {
  valor: Periodo
  onMudar: (periodo: Periodo) => void
  rotulo?: string
  id?: string
}) {
  return (
    <div className="field w-[190px]">
      <label htmlFor={id}>{rotulo}</label>
      <select
        id={id}
        className="input"
        style={{ borderRadius: 0 }}
        value={valor}
        onChange={(e) => onMudar(e.target.value as Periodo)}
      >
        {PERIODOS.map((p) => (
          <option key={p.valor} value={p.valor}>
            {p.rotulo}
          </option>
        ))}
      </select>
    </div>
  )
}
