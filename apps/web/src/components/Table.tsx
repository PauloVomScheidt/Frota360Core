import { useEffect, type FormEvent, type ReactNode } from 'react'
import { mensagensDeErro } from '../api/errors'
import { ErrorList } from './AppLayout'
import { PencilIcon, TrashIcon } from './icons'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

/** Fecha o diálogo no Escape — todo modal da aplicação deve ter essa saída. */
function useFecharComEscape(onFechar: () => void) {
  useEffect(() => {
    function aoTeclar(e: KeyboardEvent) {
      if (e.key === 'Escape') onFechar()
    }
    document.addEventListener('keydown', aoTeclar)
    return () => document.removeEventListener('keydown', aoTeclar)
  }, [onFechar])
}

/** Painel de cadastro que abre acima da tabela (padrão "Novo X" do design). */
export function InlineForm({
  onSubmit,
  children,
}: {
  onSubmit: (e: FormEvent) => void
  children: ReactNode
}) {
  return (
    <form
      onSubmit={onSubmit}
      className="mb-8 flex flex-wrap items-end gap-4 p-5"
      style={{ border: '1px solid var(--color-divider)', background: 'var(--color-surface)' }}
    >
      {children}
    </form>
  )
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
        <td colSpan={colSpan} style={{ color: '#a03123' }}>
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
  useFecharComEscape(onCancelar)

  return (
    <div className="dialog-backdrop" role="presentation" onClick={onCancelar}>
      <div
        className="dialog"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="dialog-titulo"
        onClick={(e) => e.stopPropagation()}
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
      </div>
    </div>
  )
}

/**
 * Diálogo com formulário (concluir uma manutenção, encerrar uma rota). Diferente do
 * `ConfirmDialog`, que só confirma uma ação destrutiva, aqui há campos a preencher.
 */
export function FormDialog({
  titulo,
  descricao,
  textoConfirmar,
  textoPendente,
  pending,
  erros,
  onSubmit,
  onCancelar,
  children,
}: {
  titulo: string
  descricao?: string
  textoConfirmar: string
  textoPendente: string
  pending: boolean
  erros: string[]
  onSubmit: (e: FormEvent) => void
  onCancelar: () => void
  children: ReactNode
}) {
  useFecharComEscape(onCancelar)

  return (
    <div className="dialog-backdrop" role="presentation" onClick={onCancelar}>
      <form
        className="dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="form-dialog-titulo"
        style={{ width: 'min(520px, 100%)' }}
        onClick={(e) => e.stopPropagation()}
        onSubmit={onSubmit}
      >
        <h3 id="form-dialog-titulo" className="dialog-title">
          {titulo}
        </h3>
        {descricao && <p className="dialog-body">{descricao}</p>}
        <div className="flex flex-wrap items-end gap-4">{children}</div>
        <ErrorList mensagens={erros} />
        <div className="dialog-actions">
          <button
            type="button"
            className="btn btn-secondary"
            style={{ borderRadius: 0, padding: '10px 18px' }}
            onClick={onCancelar}
            disabled={pending}
          >
            Cancelar
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            style={{ borderRadius: 0, padding: '10px 18px' }}
            disabled={pending}
          >
            {pending ? textoPendente : textoConfirmar}
          </button>
        </div>
      </form>
    </div>
  )
}
