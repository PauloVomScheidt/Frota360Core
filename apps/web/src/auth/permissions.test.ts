import { describe, expect, it } from 'vitest'
import type { Role } from '../api/types'
import { DESCRICAO_ROLE, pode, rotaInicial, ROLES } from './permissions'

type Entrada = keyof typeof pode
type Sujeito = Role | 'deslogado'

const SUJEITOS: Sujeito[] = ['Admin', 'Supervisor', 'Operador', 'Motorista', 'deslogado']

const SO_ADMIN: Sujeito[] = ['Admin']
const ADMIN_SUPERVISOR: Sujeito[] = ['Admin', 'Supervisor']
const GESTAO: Sujeito[] = ['Admin', 'Supervisor', 'Operador']
const TODOS_LOGADOS: Sujeito[] = ['Admin', 'Supervisor', 'Operador', 'Motorista']
const SO_MOTORISTA: Sujeito[] = ['Motorista']

/**
 * A matriz do CLAUDE.md da raiz e do §7 de docs/contexto-web.md, escrita a partir da
 * documentação — não copiada do código. Quem pode, por entrada de `pode`.
 */
const MATRIZ: Record<Entrada, Sujeito[]> = {
  gerenciarUsuarios: SO_ADMIN,
  gerenciarConvites: SO_ADMIN,
  verAuditoria: SO_ADMIN,

  excluir: ADMIN_SUPERVISOR,
  editarCadastros: ADMIN_SUPERVISOR,
  editarManutencoes: ADMIN_SUPERVISOR,
  editarTiposManutencao: ADMIN_SUPERVISOR,
  editarTiposDespesa: ADMIN_SUPERVISOR,
  editarTiposCombustivel: ADMIN_SUPERVISOR,
  editarPostos: ADMIN_SUPERVISOR,

  verDashboard: GESTAO,
  verMotoristas: GESTAO,
  verRotas: GESTAO,
  editarRotas: GESTAO,
  verCustos: GESTAO,
  verDespesas: GESTAO,
  lancarDespesa: GESTAO,

  verVeiculos: TODOS_LOGADOS,
  verManutencoes: TODOS_LOGADOS,
  verAbastecimentos: TODOS_LOGADOS,
  lancarAbastecimento: TODOS_LOGADOS,

  verMinhasRotas: SO_MOTORISTA,
}

const CENARIOS = (Object.entries(MATRIZ) as [Entrada, Sujeito[]][]).flatMap(([entrada, permitidos]) =>
  SUJEITOS.map((sujeito) => ({ entrada, sujeito, permitido: permitidos.includes(sujeito) })),
)

const papel = (sujeito: Sujeito): Role | undefined => (sujeito === 'deslogado' ? undefined : sujeito)

describe('matriz de permissões', () => {
  it('a tabela cobre toda entrada de `pode` — permissão nova sem cenário reprova aqui', () => {
    expect(Object.keys(MATRIZ).sort()).toEqual(Object.keys(pode).sort())
  })

  it.each(CENARIOS)('pode.$entrada para $sujeito → $permitido', ({ entrada, sujeito, permitido }) => {
    expect(pode[entrada](papel(sujeito))).toBe(permitido)
  })
})

describe('regras transversais da matriz', () => {
  it('exclusão acompanha edição: Admin e Supervisor excluem, Operador e Motorista não', () => {
    expect(SUJEITOS.filter((s) => pode.excluir(papel(s)))).toEqual(['Admin', 'Supervisor'])
    expect(SUJEITOS.filter((s) => pode.excluir(papel(s)))).toEqual(
      SUJEITOS.filter((s) => pode.editarCadastros(papel(s))),
    )
  })

  it('usuários, convites e auditoria são exclusivos do Admin', () => {
    for (const entrada of ['gerenciarUsuarios', 'gerenciarConvites', 'verAuditoria'] as const) {
      expect(SUJEITOS.filter((s) => pode[entrada](papel(s)))).toEqual(['Admin'])
    }
  })

  it('usuário deslogado não pode nada', () => {
    expect(Object.values(pode).some((predicado) => predicado(undefined))).toBe(false)
  })

  it('o motorista não enxerga nenhuma tela da gestão além de veículos, manutenções e abastecimentos', () => {
    const telasDoMotorista = (Object.keys(pode) as Entrada[]).filter(
      (e) => e.startsWith('ver') && pode[e]('Motorista'),
    )
    expect(telasDoMotorista.sort()).toEqual(['verAbastecimentos', 'verManutencoes', 'verMinhasRotas', 'verVeiculos'])
  })
})

describe('rotaInicial', () => {
  it('o motorista cai nas próprias rotas, nunca no dashboard', () => {
    expect(rotaInicial('Motorista')).toBe('/minhas-rotas')
  })

  it.each<Role>(['Admin', 'Supervisor', 'Operador'])('%s cai no dashboard', (role) => {
    expect(rotaInicial(role)).toBe('/dashboard')
  })

  it('o destino de cada papel é uma tela que ele pode ver (sem pingue-pongue entre guards)', () => {
    expect(pode.verMinhasRotas('Motorista') && rotaInicial('Motorista') === '/minhas-rotas').toBe(true)
    for (const role of ['Admin', 'Supervisor', 'Operador'] as Role[]) {
      expect(pode.verDashboard(role)).toBe(true)
    }
  })
})

describe('papéis', () => {
  it('são quatro, e cada um tem descrição', () => {
    expect(ROLES).toEqual(['Admin', 'Supervisor', 'Operador', 'Motorista'])
    expect(ROLES.every((r) => DESCRICAO_ROLE[r].length > 0)).toBe(true)
  })
})
