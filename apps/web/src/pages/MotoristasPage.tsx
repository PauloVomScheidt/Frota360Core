import { useQuery } from '@tanstack/react-query'
import { motoristasApi } from '../api/motoristas'
import { AppLayout, PageHeader } from '../components/AppLayout'
import { Paginacao, TableStates } from '../components/Table'
import { usePaginacao } from '../lib/paginacao'
import { formatCpf, formatDate } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

/**
 * Somente leitura. Não há cadastro de motorista: a lista é a projeção dos usuários
 * com a role Motorista, então quem concede e remove o acesso são `/convites` e
 * `/usuarios` — cadastrar aqui criaria de novo a duplicação que o modelo eliminou.
 */
export function MotoristasPage() {
  const motoristasQuery = useQuery({ queryKey: ['motoristas'], queryFn: motoristasApi.getAll })

  const motoristas = motoristasQuery.data ?? []
  const p = usePaginacao(motoristas)

  return (
    <AppLayout>
      <PageHeader
        titulo="Motoristas"
        subtitulo="Usuários com o perfil Motorista. Para conceder o acesso, envie um convite; para trocar ou remover, use Usuários."
      />

      <div className="overflow-x-auto">
        <table className="table">
          <thead>
            <tr>
              <th>Nome</th>
              <th>E-mail</th>
              <th>CPF</th>
              <th>Nascimento</th>
              <th>Status</th>
              <th>Desde</th>
            </tr>
          </thead>
          <tbody>
            <TableStates
              colSpan={6}
              pending={motoristasQuery.isPending}
              error={motoristasQuery.error}
              empty={motoristasQuery.isSuccess && motoristas.length === 0}
              textoCarregando="Carregando motoristas…"
              textoErro="Não foi possível carregar os motoristas."
              textoVazio="Nenhum usuário com o perfil Motorista ainda."
            />
            {p.itensDaPagina.map((m) => (
              <tr key={m.id}>
                <td className="font-semibold">{m.nome}</td>
                <td>{m.email}</td>
                {/* Opcionais: só existem se a pessoa os informou ao aceitar o convite. */}
                <td>{m.cpf ? formatCpf(m.cpf) : '—'}</td>
                <td>{formatDate(m.dataNascimento)}</td>
                <td>
                  {/* Verde, não azul: cadastro ativo é estado saudável, não algo
                      acontecendo agora — o azul fica reservado a rota/manutenção em curso. */}
                  <span className={m.ativo ? 'tag tag-success' : 'tag tag-neutral'}>
                    {m.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td>{formatDate(m.dataInclusao)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Paginacao {...p} pending={motoristasQuery.isFetching} />

      <p className="mt-4 text-[13px]" style={{ color: mutedText }}>
        Um motorista é um usuário com esse perfil — abre e encerra as próprias rotas em "Minhas
        rotas". CPF e nascimento são opcionais e vêm do que a pessoa informou ao criar a conta.
      </p>
    </AppLayout>
  )
}
