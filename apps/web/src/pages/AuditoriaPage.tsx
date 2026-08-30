import { Fragment, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { auditoriaApi } from '../api/auditoria'
import { usuariosApi } from '../api/usuarios'
import type {
  AcaoAuditoria,
  AuditoriaFiltro,
  EntidadeAuditada,
  LogAuditoriaResponse,
} from '../api/types'
import { AppLayout, PageHeader } from '../components/AppLayout'
import { Paginacao, TableStates } from '../components/Table'
import { ChevronDownIcon, ChevronRightIcon } from '../components/icons'
import { formatDateTime } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

const TAMANHO_PAGINA = 25

/** Rótulos legíveis para o vocabulário fechado da API (§ Auditoria do contexto). */
const ROTULO_ENTIDADE: Record<EntidadeAuditada, string> = {
  Veiculo: 'Veículo',
  Rota: 'Rota',
  Manutencao: 'Manutenção',
  Abastecimento: 'Abastecimento',
  TipoManutencao: 'Tipo de manutenção',
  Usuario: 'Usuário',
  Convite: 'Convite',
}

const ROTULO_ACAO: Record<AcaoAuditoria, string> = {
  Criou: 'Criou',
  Atualizou: 'Atualizou',
  Excluiu: 'Excluiu',
  Encerrou: 'Encerrou',
  Concluiu: 'Concluiu',
  AlterouPermissao: 'Alterou permissão',
  Ativou: 'Ativou',
  Desativou: 'Desativou',
  Cancelou: 'Cancelou',
  Aceitou: 'Aceitou',
}

/**
 * A cor sinaliza a consequência, não a entidade — mesmo vocabulário das outras telas:
 * vermelho apaga ou remove acesso, âmbar mexe em permissão, azul cria, verde conclui.
 * `Atualizou` fica neutro de propósito: é a ação mais comum da trilha, e colori-la
 * afogaria o resto numa tabela longa.
 */
function classeDaAcao(acao: AcaoAuditoria): string {
  if (acao === 'Excluiu' || acao === 'Desativou') return 'tag tag-danger'
  if (acao === 'AlterouPermissao' || acao === 'Cancelou') return 'tag tag-warning'
  if (acao === 'Criou') return 'tag tag-accent'
  if (acao === 'Concluiu' || acao === 'Encerrou' || acao === 'Ativou' || acao === 'Aceitou')
    return 'tag tag-success'
  return 'tag tag-neutral'
}

/**
 * Os valores do diff chegam em cultura invariante (a API os grava assim de propósito,
 * para que o histórico não dependa de quem escreveu). Datas ISO viram pt-BR na leitura;
 * o resto passa direto.
 */
function formatarValor(valor: string | null | undefined): string {
  if (valor === null || valor === undefined || valor === '') return '—'
  return /^\d{4}-\d{2}-\d{2}T/.test(valor) ? formatDateTime(valor) : valor
}

export function AuditoriaPage() {
  const [pagina, setPagina] = useState(1)
  const [filtroEntidade, setFiltroEntidade] = useState('')
  const [filtroAcao, setFiltroAcao] = useState('')
  const [filtroUsuario, setFiltroUsuario] = useState('')
  const [de, setDe] = useState('')
  const [ate, setAte] = useState('')

  // Linhas com diff abrem sob demanda: a descrição resolve a maioria das consultas,
  // e o campo-a-campo é o detalhe de quem está investigando um caso específico.
  const [expandida, setExpandida] = useState<number | null>(null)

  const filtro: AuditoriaFiltro = {
    pagina,
    tamanhoPagina: TAMANHO_PAGINA,
    entidade: filtroEntidade === '' ? undefined : (filtroEntidade as EntidadeAuditada),
    acao: filtroAcao === '' ? undefined : (filtroAcao as AcaoAuditoria),
    usuarioId: filtroUsuario === '' ? undefined : Number(filtroUsuario),
    de: de === '' ? undefined : de,
    ate: ate === '' ? undefined : ate,
  }

  const auditoriaQuery = useQuery({
    queryKey: ['auditoria', filtro],
    queryFn: () => auditoriaApi.consultar(filtro),
  })

  // A tela é Admin-only, então o endpoint de usuários está sempre disponível aqui.
  const usuariosQuery = useQuery({ queryKey: ['usuarios'], queryFn: usuariosApi.getAll })

  const dados = auditoriaQuery.data
  const logs = dados?.itens ?? []
  const usuarios = usuariosQuery.data ?? []

  const temFiltro = filtroEntidade !== '' || filtroAcao !== '' || filtroUsuario !== '' || de !== '' || ate !== ''

  /** Qualquer mudança de filtro volta para a primeira página — senão a tela abre vazia. */
  function aplicar(mudanca: () => void) {
    mudanca()
    setPagina(1)
    setExpandida(null)
  }

  function limparFiltros() {
    aplicar(() => {
      setFiltroEntidade('')
      setFiltroAcao('')
      setFiltroUsuario('')
      setDe('')
      setAte('')
    })
  }

  function alternarLinha(log: LogAuditoriaResponse) {
    if (log.alteracoes.length === 0) return
    setExpandida((atual) => (atual === log.id ? null : log.id))
  }

  return (
    <AppLayout>
      <PageHeader
        titulo="Auditoria"
        subtitulo="Tudo que foi alterado na empresa, do mais recente para o mais antigo. Somente leitura — nem o administrador apaga uma linha."
        acoes={
          <button
            type="button"
            className="btn btn-secondary"
            style={{ borderRadius: 0 }}
            onClick={() => auditoriaQuery.refetch()}
            disabled={auditoriaQuery.isFetching}
          >
            {auditoriaQuery.isFetching ? 'Atualizando…' : 'Atualizar'}
          </button>
        }
      />

      <div className="mb-5 flex flex-wrap items-end gap-4">
        <div className="field w-[190px]">
          <label htmlFor="filtroEntidade">O que</label>
          <select
            id="filtroEntidade"
            className="input"
            style={{ borderRadius: 0 }}
            value={filtroEntidade}
            onChange={(e) => aplicar(() => setFiltroEntidade(e.target.value))}
          >
            <option value="">Tudo</option>
            {(Object.keys(ROTULO_ENTIDADE) as EntidadeAuditada[]).map((entidade) => (
              <option key={entidade} value={entidade}>
                {ROTULO_ENTIDADE[entidade]}
              </option>
            ))}
          </select>
        </div>

        <div className="field w-[190px]">
          <label htmlFor="filtroAcao">Ação</label>
          <select
            id="filtroAcao"
            className="input"
            style={{ borderRadius: 0 }}
            value={filtroAcao}
            onChange={(e) => aplicar(() => setFiltroAcao(e.target.value))}
          >
            <option value="">Todas</option>
            {(Object.keys(ROTULO_ACAO) as AcaoAuditoria[]).map((acao) => (
              <option key={acao} value={acao}>
                {ROTULO_ACAO[acao]}
              </option>
            ))}
          </select>
        </div>

        <div className="field w-[230px]">
          <label htmlFor="filtroUsuario">Quem</label>
          <select
            id="filtroUsuario"
            className="input"
            style={{ borderRadius: 0 }}
            value={filtroUsuario}
            onChange={(e) => aplicar(() => setFiltroUsuario(e.target.value))}
          >
            <option value="">Qualquer pessoa</option>
            {usuarios.map((u) => (
              <option key={u.id} value={u.id}>
                {u.nome} ({u.email})
              </option>
            ))}
          </select>
        </div>

        <div className="field w-[160px]">
          <label htmlFor="de">De</label>
          <input
            id="de"
            type="date"
            className="input"
            style={{ borderRadius: 0 }}
            value={de}
            onChange={(e) => aplicar(() => setDe(e.target.value))}
          />
        </div>

        <div className="field w-[160px]">
          <label htmlFor="ate">Até</label>
          <input
            id="ate"
            type="date"
            className="input"
            style={{ borderRadius: 0 }}
            value={ate}
            onChange={(e) => aplicar(() => setAte(e.target.value))}
          />
        </div>

        {temFiltro && (
          <button
            type="button"
            className="btn btn-secondary"
            style={{ borderRadius: 0, padding: '10px 18px' }}
            onClick={limparFiltros}
          >
            Limpar filtros
          </button>
        )}
      </div>

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th style={{ width: 40 }} aria-label="Detalhes" />
              <th>Quando</th>
              <th>Quem</th>
              <th>Ação</th>
              <th>Registro</th>
              <th>O que aconteceu</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={6}
              pending={auditoriaQuery.isPending}
              error={auditoriaQuery.error}
              empty={logs.length === 0}
              textoErro="Não foi possível carregar a auditoria."
              textoVazio={
                temFiltro
                  ? 'Nenhum registro para os filtros escolhidos.'
                  : 'Nada registrado ainda. A trilha começa na próxima alteração feita no sistema.'
              }
            />
            {logs.map((log) => {
              const temDiff = log.alteracoes.length > 0
              const aberta = expandida === log.id

              return (
                <Fragment key={log.id}>
                  <tr
                    onClick={() => alternarLinha(log)}
                    style={{ cursor: temDiff ? 'pointer' : 'default' }}
                  >
                    <td style={{ color: mutedText }}>
                      {/* Sem diff não há o que abrir — a seta some em vez de enganar. */}
                      {temDiff &&
                        (aberta ? <ChevronDownIcon size={15} /> : <ChevronRightIcon size={15} />)}
                    </td>
                    <td style={{ whiteSpace: 'nowrap' }}>{formatDateTime(log.dataHora)}</td>
                    <td>
                      <div>{log.usuarioNome}</div>
                      {/* O papel é o do momento da ação, não o atual — vem gravado na linha. */}
                      <div className="text-[11px] uppercase" style={{ letterSpacing: '0.06em', color: mutedText }}>
                        {log.usuarioRole}
                      </div>
                    </td>
                    <td>
                      <span className={classeDaAcao(log.acao)}>{ROTULO_ACAO[log.acao]}</span>
                    </td>
                    <td style={{ whiteSpace: 'nowrap' }}>
                      {ROTULO_ENTIDADE[log.entidade]}
                      {log.entidadeId != null && (
                        <span style={{ color: mutedText }}> #{log.entidadeId}</span>
                      )}
                    </td>
                    <td>{log.descricao}</td>
                  </tr>

                  {aberta && (
                    <tr>
                      <td />
                      <td colSpan={5} style={{ background: 'var(--color-surface)' }}>
                        <div className="flex flex-col gap-1 py-1">
                          {log.alteracoes.map((alteracao, i) => (
                            <div key={`${log.id}-${i}`} className="text-[13px]">
                              <strong>{alteracao.campo}</strong>{' '}
                              <span style={{ color: mutedText }}>{formatarValor(alteracao.de)}</span>
                              <span style={{ color: mutedText }}> → </span>
                              <span>{formatarValor(alteracao.para)}</span>
                            </div>
                          ))}
                          {log.ipOrigem && (
                            <div className="mt-1 text-[12px]" style={{ color: mutedText }}>
                              Origem: {log.ipOrigem}
                            </div>
                          )}
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              )
            })}
          </tbody>
        </table>
      </div>

      {dados && (
        <Paginacao
          pagina={dados.pagina}
          totalPaginas={dados.totalPaginas}
          total={dados.total}
          tamanhoPagina={dados.tamanhoPagina}
          onMudar={(p) => {
            setPagina(p)
            setExpandida(null)
          }}
          pending={auditoriaQuery.isFetching}
        />
      )}
    </AppLayout>
  )
}
