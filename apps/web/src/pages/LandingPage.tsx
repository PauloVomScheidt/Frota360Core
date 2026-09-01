import { useEffect, useRef, useState, type CSSProperties, type FormEvent, type ReactNode, type RefObject } from 'react'
import { Link } from 'react-router-dom'
import { LogoMark, Wordmark } from '../components/Logo'
import { CheckIcon, SearchIcon, WhatsappIcon, XIcon } from '../components/icons'
import '../styles/landing.css'

const WHATSAPP = '5547991120404'
const EMAIL = 'phsvscheidt2003@gmail.com'
const LINK_WHATS = `https://wa.me/${WHATSAPP}`
const LINK_EMAIL = `mailto:${EMAIL}`

// ── conteúdo ────────────────────────────────────────────────────────────────
// Na ordem em que aparecem na página — é o que dá a `ANCORAS` sua ordem certa.

const ANCORAS = [
  { href: '#recursos', texto: 'Recursos' },
  { href: '#como-funciona', texto: 'Como funciona' },
  { href: '#manutencao', texto: 'Manutenção' },
  { href: '#permissoes', texto: 'Permissões' },
  { href: '#duvidas', texto: 'Dúvidas' },
]

const MENU_MOCK = ['Visão geral', 'Motoristas', 'Veículos', 'Rotas', 'Manutenções', 'Usuários']
const MENU_ATIVO = 'Veículos'

type Tom = 'painel' | 'neutro' | 'vencendo' | 'alerta'

// Mesma classe `.tag`/`.tag-*` que a UI real usa — os mocks não inventam um
// visual próprio para situação.
const CLASSE_ETIQ: Record<Tom, string> = {
  painel: 'tag tag-accent',
  neutro: 'tag tag-neutral',
  vencendo: 'tag tag-warning',
  alerta: 'tag tag-danger',
}

const VEICULOS_MOCK: { placa: string; modelo: string; km: string; motorista: string; situacao: string; tom: Tom }[] = [
  { placa: 'MHT-4G21', modelo: 'Ford Cargo 816', km: '148.230', motorista: 'Ana Ribeiro', situacao: 'Em rota', tom: 'painel' },
  { placa: 'LZP-9D14', modelo: 'VW Constellation', km: '312.940', motorista: 'Ivo Nascimento', situacao: 'Em rota', tom: 'painel' },
  { placa: 'QKA-7B08', modelo: 'VW Delivery', km: '96.510', motorista: 'Carlos Deppe', situacao: 'Disponível', tom: 'neutro' },
  { placa: 'RTB-2C55', modelo: 'Mercedes Sprinter', km: '74.180', motorista: 'Marta Lins', situacao: 'Disponível', tom: 'neutro' },
  { placa: 'MJU-5F71', modelo: 'VW Saveiro', km: '51.988', motorista: 'Helena Cruz', situacao: 'Revisão', tom: 'vencendo' },
  { placa: 'PGE-1H47', modelo: 'Fiat Fiorino', km: '38.420', motorista: 'Régis Alves', situacao: 'Disponível', tom: 'neutro' },
]

const FUNDACOES = [
  'Multiempresa desde o primeiro dia',
  'Convites com papel definido',
  'Sessão revogada na hora',
  'Manutenção preventiva por km',
  'Validação de CPF e idade',
]

const STATS = [
  { valor: '1–500', rotulo: 'Veículos por empresa, sem mudar de plano' },
  { valor: '3', rotulo: 'Papéis de acesso prontos para usar' },
  { valor: '1 semana', rotulo: 'Do primeiro contato ao painel rodando' },
  { valor: '100%', rotulo: 'Isolamento dos dados entre empresas' },
]

const DORES = [
  { num: '01', texto: 'A quilometragem de cada veículo mora em três lugares — e nenhum deles está atualizado.' },
  { num: '02', texto: 'Ninguém sabe de cabeça quem estava com o caminhão na última viagem.' },
  { num: '03', texto: 'A planilha é compartilhada com a equipe toda, então qualquer um apaga qualquer coisa.' },
  { num: '04', texto: 'CPF errado, motorista duplicado, cadastro de gente que já saiu da empresa.' },
]

const COMPARATIVO = [
  {
    item: 'Quilometragem atual do veículo',
    planilha: 'Depende de alguém lembrar de atualizar',
    app: 'Fica no cadastro do veículo, sempre visível',
  },
  { item: 'Quem rodou por último', planilha: 'Some no histórico de versões', app: 'Registrado na rota e no veículo' },
  { item: 'Quem pode apagar dados', planilha: 'Qualquer um com o link', app: 'Só o administrador' },
  { item: 'Motorista duplicado', planilha: 'Ninguém percebe', app: 'CPF e e-mail únicos, bloqueado no cadastro' },
  { item: 'Alguém saiu da empresa', planilha: 'O acesso continua', app: 'Usuário desativado, sessão cai na hora' },
]

const RECURSOS = [
  {
    inicial: 'M',
    titulo: 'Motoristas',
    texto: 'Cadastro validado de verdade: CPF conferido dígito a dígito e idade mínima checada na hora.',
    itens: [
      'E-mail e CPF únicos na sua empresa',
      'Histórico de admissão por motorista',
      'Vínculo direto com as rotas rodadas',
    ],
  },
  {
    inicial: 'V',
    titulo: 'Veículos',
    texto: 'Placa, marca, quilometragem e o rastro de quem rodou por último com ele.',
    itens: ['Quilometragem sempre no cadastro', 'Último motorista e última viagem', 'Frota inteira em uma lista só'],
  },
  {
    inicial: 'R',
    titulo: 'Rotas',
    texto: 'Origem, destino, motorista e veículo — abertas ou encerradas, com datas.',
    itens: ['Rota ativa x encerrada', 'Motorista e veículo vinculados', 'Qualquer membro da equipe pode lançar'],
  },
  {
    inicial: 'P',
    titulo: 'Manutenções',
    texto: 'Manutenção preventiva por quilometragem: você agenda, o painel avisa quando está vencendo.',
    itens: [
      'Pendentes primeiro, vencendo no topo',
      'Catálogo de tipos com intervalo em km',
      'Concluir já atualiza a km do veículo',
    ],
  },
]

// Aqui a numeração é real: os passos acontecem nesta ordem.
const PASSOS = [
  {
    num: '1',
    titulo: 'A gente conversa',
    texto: 'Você mostra como controla a frota hoje e o que precisa sair da planilha primeiro.',
  },
  {
    num: '2',
    titulo: 'Configuramos sua empresa',
    texto: 'Criamos o ambiente da sua empresa e o acesso do primeiro administrador. Nada de formulário público.',
  },
  {
    num: '3',
    titulo: 'Você convida a equipe',
    texto: 'Convite por e-mail com o papel já definido. A pessoa escolhe a senha e entra direto no painel.',
  },
]

const ROTAS_MOCK: { trecho: string; motorista: string; veiculo: string; inicio: string; situacao: string; tom: Tom }[] = [
  { trecho: 'Joinville → Curitiba', motorista: 'Ana Ribeiro', veiculo: 'MHT-4G21', inicio: '22/08 06:10', situacao: 'Em curso', tom: 'painel' },
  { trecho: 'Pátio → Blumenau', motorista: 'Carlos Deppe', veiculo: 'QKA-7B08', inicio: '22/08 07:40', situacao: 'Em curso', tom: 'painel' },
  { trecho: 'CD Norte → Itajaí', motorista: 'Marta Lins', veiculo: 'RTB-2C55', inicio: '21/08 13:05', situacao: 'Encerrada', tom: 'neutro' },
  { trecho: 'Joinville → Porto Itapoá', motorista: 'Ivo Nascimento', veiculo: 'LZP-9D14', inicio: '21/08 05:30', situacao: 'Encerrada', tom: 'neutro' },
  { trecho: 'São Bento → Pátio', motorista: 'Helena Cruz', veiculo: 'MJU-5F71', inicio: '20/08 16:20', situacao: 'Encerrada', tom: 'neutro' },
]

const MANUTENCOES_MOCK: {
  placa: string
  modelo: string
  tipo: string
  km: string
  andamento: string
  situacao: string
  tom: Tom
}[] = [
  // A ordem é o argumento da seção: atrasada primeiro, depois vencendo, e o
  // que está em dia no fim.
  { placa: 'RTB-2C55', modelo: 'Sprinter · 74.180 km', tipo: 'Troca de óleo', km: '73.500', andamento: 'atrasada 680 km', situacao: 'Atrasada', tom: 'alerta' },
  { placa: 'MJU-5F71', modelo: 'Saveiro · 51.988 km', tipo: 'Troca de óleo', km: '52.000', andamento: 'faltam 12 km', situacao: 'Vencendo', tom: 'vencendo' },
  { placa: 'QRS-5T90', modelo: 'HB20 · 9.850 km', tipo: 'Revisão', km: '10.000', andamento: 'faltam 150 km', situacao: 'Vencendo', tom: 'vencendo' },
  { placa: 'LZP-9D14', modelo: 'Constellation · 312.940 km', tipo: 'Troca de pneus', km: '315.000', andamento: 'faltam 2.060 km', situacao: 'Pendente', tom: 'painel' },
  { placa: 'ABC-1D23', modelo: 'Civic 2.0 · 65.200 km', tipo: 'Troca de óleo', km: '55.200', andamento: 'concluída 25/08 · R$ 500,00', situacao: 'Concluída', tom: 'neutro' },
]

const TIPOS_MOCK = [
  { nome: 'Troca de óleo', intervalo: '10.000 km' },
  { nome: 'Revisão', intervalo: '10.000 km' },
  { nome: 'Filtro de ar-condicionado', intervalo: '50.000 km' },
]

// Mesma ordem das colunas do <thead> da matriz — dá a cada coluna uma
// identidade estável (o nome do papel), em vez de depender da posição.
const PAPEIS_DA_MATRIZ = ['admin', 'supervisor', 'operador'] as const

const MATRIZ = [
  { acao: 'Ver tudo da empresa', admin: true, supervisor: true, operador: true },
  { acao: 'Lançar e editar rotas', admin: true, supervisor: true, operador: true },
  { acao: 'Cadastrar motoristas e veículos', admin: true, supervisor: true, operador: false },
  { acao: 'Agendar e concluir manutenções', admin: true, supervisor: true, operador: false },
  { acao: 'Excluir qualquer registro', admin: true, supervisor: false, operador: false },
  { acao: 'Convidar e gerenciar usuários', admin: true, supervisor: false, operador: false },
]

const SEGURANCA = [
  {
    kicker: 'Isolamento',
    titulo: 'Sua empresa, seus dados',
    texto: 'O acesso de cada usuário carrega a empresa dele. Dado de outra empresa simplesmente não existe para ele.',
  },
  {
    kicker: 'Sessões',
    titulo: 'Saída imediata',
    texto: 'Tirar o acesso de alguém ou trocar o papel dele derruba a sessão aberta na hora.',
  },
  {
    kicker: 'Entrada',
    titulo: 'Só quem foi convidado',
    texto: 'Não existe cadastro aberto. Toda conta nasce de um convite com validade e uso único.',
  },
]

const OBJECOES = [
  {
    q: '“Tenho só 8 veículos, é grande demais para mim.”',
    r: 'O painel é o mesmo para 8 ou 300 veículos. Com frota pequena a implantação leva um dia — e são justamente esses casos que a planilha bagunça primeiro.',
  },
  {
    q: '“Meu motorista não vai usar aplicativo.”',
    r: 'Ele não precisa. Quem lança rota é o escritório; o motorista só aparece como cadastro. Se um dia quiser, ele entra como operador.',
  },
  {
    q: '“Minha planilha funciona bem.”',
    r: 'Funciona até duas pessoas mexerem no mesmo dia. Você pode começar importando exatamente essa planilha — sem retrabalho.',
  },
]

const DUVIDAS = [
  { p: 'Preciso instalar algo?', r: 'Não. O painel roda no navegador — computador do escritório, notebook ou celular.' },
  {
    p: 'Como minha equipe entra?',
    r: 'O administrador envia um convite por e-mail com o papel já definido. A pessoa cria a senha e cai direto no painel.',
  },
  {
    p: 'E se alguém sair da empresa?',
    r: 'O administrador desativa o usuário. O acesso morre na hora, e o histórico de rotas continua intacto.',
  },
  {
    p: 'Serve para frota interna, não rodoviária?',
    r: 'Serve. Rota é origem, destino, motorista e veículo — funciona igual entre pátio e galpão.',
  },
  {
    p: 'Dá para levar meus dados atuais?',
    r: 'Sim. Na implantação a gente importa a sua planilha de motoristas e veículos junto com você.',
  },
  {
    p: 'Como o sistema sabe que a revisão está vencendo?',
    r: 'Você agenda a manutenção na quilometragem prevista e o painel compara com a km atual do veículo. O que está vencendo ou atrasado sobe para o topo da lista.',
  },
  {
    p: 'Quanto custa?',
    r: 'O preço depende do tamanho da frota e de quantas pessoas vão usar. A gente fecha isso na conversa inicial.',
  },
]

const TAMANHOS_FROTA = ['Até 10 veículos', '11 a 50 veículos', '51 a 200 veículos', 'Mais de 200 veículos']

function prefereMenosMovimento() {
  return window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
}

// ── revelar ao rolar ────────────────────────────────────────────────────────

/**
 * Sob rolagem, cada faixa (exceto o hero e o mock do painel, sempre visíveis)
 * nasce oculta e aparece ao entrar na viewport. Quem prefere menos movimento
 * não recebe sequer a classe que começa oculta — não é uma questão de pular a
 * transição, é de nunca esconder o conteúdo pra essa pessoa.
 */
function useRevelarFaixas(raizRef: RefObject<HTMLDivElement | null>) {
  useEffect(() => {
    if (prefereMenosMovimento() || !window.IntersectionObserver) return
    const raiz = raizRef.current
    if (!raiz) return
    const faixas = Array.from(raiz.querySelectorAll<HTMLElement>('.lp-faixa')).slice(1)
    if (!faixas.length) return
    faixas.forEach((el) => el.classList.add('lp-revela'))
    const io = new IntersectionObserver(
      (entradas) => {
        entradas.forEach((e) => {
          if (!e.isIntersecting) return
          e.target.classList.add('is-visivel')
          io.unobserve(e.target)
        })
      },
      { rootMargin: '0px 0px -12% 0px' },
    )
    faixas.forEach((el) => io.observe(el))
    return () => io.disconnect()
  }, [raizRef])
}

// ── peças ───────────────────────────────────────────────────────────────────

function Marca({ tamanho = 22 }: { tamanho?: number }) {
  return (
    <>
      <LogoMark size={tamanho} />
      <Wordmark size={16} />
    </>
  )
}

function Faixa({
  id,
  estreito = false,
  espaco = 120,
  children,
}: {
  id?: string
  estreito?: boolean
  espaco?: number
  children: ReactNode
}) {
  return (
    <section
      id={id}
      className="lp-faixa"
      style={{ '--topo': `${espaco}px`, ...(id ? { scrollMarginTop: 96 } : {}) } as CSSProperties}
    >
      <div className={estreito ? 'lp-wrap lp-wrap-estreito' : 'lp-wrap'}>{children}</div>
    </section>
  )
}

/** Região rolável precisa ser focável para quem navega só pelo teclado. */
function Rolagem({ rotulo, children }: { rotulo: string; children: ReactNode }) {
  return (
    <div className="lp-rolagem" tabIndex={0} role="region" aria-label={rotulo}>
      {children}
    </div>
  )
}

/**
 * Moldura de vitrine — arredondada, com sombra — em volta de um mock reto do
 * painel de verdade. O `.lp-dispositivo` é a moldura; o `.lp-painel` de
 * dentro reaproveita as mesmas classes globais (`.table`, `.tag`, `.btn`) que
 * a UI real usa, porque ele PRECISA parecer com o produto — ver
 * docs/contexto-web.md §3.1.
 */
function Dispositivo({ comMenu = false, children }: { comMenu?: boolean; children: ReactNode }) {
  return (
    <div className="lp-dispositivo">
      <div className={comMenu ? 'lp-painel lp-painel-com-menu' : 'lp-painel'}>{children}</div>
    </div>
  )
}

// ── seções ──────────────────────────────────────────────────────────────────
// Cada `Faixa` da página é só HTML + os arrays de conteúdo lá em cima — nenhuma
// delas guarda estado próprio. Extraídas para módulo por serem, juntas, o
// motivo de `LandingPage` passar de 300 linhas sem nenhuma delas ter lógica
// que justifique inline.

function CabecalhoLanding() {
  return (
    <header className="lp-nav">
      <Link to="/" className="lp-brand" aria-label="Frota 360 — início">
        <Marca />
      </Link>
      <nav className="lp-nav-links" aria-label="Seções da página">
        {ANCORAS.map((a) => (
          <a key={a.href} href={a.href}>
            {a.texto}
          </a>
        ))}
      </nav>
      <div className="lp-nav-acoes">
        <Link to="/login" className="lp-btn lp-btn-discreto lp-btn-p lp-nav-entrar">
          Entrar
        </Link>
        <a href={LINK_WHATS} className="lp-btn lp-btn-primario lp-btn-p">
          Falar com a gente
        </a>
        <details className="lp-menu">
          <summary aria-label="Abrir as seções da página">Seções</summary>
          <nav className="lp-menu-lista" aria-label="Seções da página">
            {ANCORAS.map((a) => (
              <a key={a.href} href={a.href}>
                {a.texto}
              </a>
            ))}
            {/* Abaixo de 560px "Entrar" some da barra; aqui é o caminho dele. */}
            <Link to="/login" className="lp-menu-entrar">
              Entrar
            </Link>
          </nav>
        </details>
      </div>
    </header>
  )
}

function HeroLanding() {
  return (
    <section className="lp-hero">
      <div className="lp-wrap">
        <div className="lp-hero-topo">
          <h1 className="lp-h1">A revisão avisa antes de virar oficina.</h1>
          <p className="lp-hero-sub">
            Você agenda a manutenção na quilometragem prevista. Conforme o veículo roda, o painel recalcula sozinho
            quantos quilômetros faltam — e põe no topo da lista o que está vencendo.
          </p>
          <div className="lp-hero-ctas">
            <a href={LINK_WHATS} className="lp-btn lp-btn-primario lp-btn-g">
              <WhatsappIcon size={17} />
              Falar no WhatsApp
            </a>
            <a href="#demonstracao" className="lp-btn lp-btn-contorno lp-btn-g">
              Pedir uma demonstração
            </a>
          </div>
          <p className="lp-miudo">
            Implantação assistida · sem cartão de crédito · <Link to="/login">já é cliente?</Link>
          </p>
        </div>
      </div>
    </section>
  )
}

/** Mock do painel: dados ilustrativos, não vêm da API. */
function PainelMockFaixa() {
  return (
    <Faixa espaco={64}>
      <span className="lp-campo">O painel</span>
      <Dispositivo comMenu>
        <aside className="lp-painel-aside">
          <div className="lp-brand" style={{ padding: '0 8px', marginRight: 0 }}>
            <Marca tamanho={20} />
          </div>
          <nav style={{ display: 'flex', flexDirection: 'column', gap: 2 }} aria-hidden="true">
            {MENU_MOCK.map((item) => (
              <div key={item} className={`lp-painel-item${item === MENU_ATIVO ? ' is-ativo' : ''}`}>
                <span className="lp-painel-quad" />
                {item}
              </div>
            ))}
          </nav>
          <div className="lp-painel-user">
            <span className="lp-painel-avatar">PD</span>
            <div>
              Paulo D.
              <div className="lp-painel-sub">Admin</div>
            </div>
          </div>
        </aside>

        <div>
          <div className="lp-painel-cab">
            <span className="lp-painel-titulo">Veículos</span>
            <span className="lp-contagem">42 cadastrados</span>
            <div className="lp-painel-direita">
              <span className="lp-painel-busca" aria-hidden="true">
                <SearchIcon size={14} />
                Buscar placa ou modelo
              </span>
              <span className="btn btn-primary" aria-hidden="true">
                Novo veículo
              </span>
            </div>
          </div>
          <Rolagem rotulo="Exemplo da lista de veículos">
            <table className="table">
              <thead>
                <tr>
                  <th>Placa</th>
                  <th>Modelo</th>
                  <th className="lp-painel-numero-dir">Quilometragem</th>
                  <th>Último motorista</th>
                  <th>Situação</th>
                </tr>
              </thead>
              <tbody>
                {VEICULOS_MOCK.map((v) => (
                  <tr key={v.placa}>
                    <td className="lp-painel-forte">{v.placa}</td>
                    <td>{v.modelo}</td>
                    <td className="lp-painel-numero lp-painel-numero-dir">{v.km}</td>
                    <td>{v.motorista}</td>
                    <td>
                      <span className={CLASSE_ETIQ[v.tom]}>{v.situacao}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Rolagem>
          <div className="lp-painel-rodape">Última atualização hoje, 14:20 — por Ana Ribeiro</div>
        </div>
      </Dispositivo>
    </Faixa>
  )
}

function FundacoesFaixa() {
  return (
    <Faixa espaco={44}>
      <div className="lp-fundacoes">
        <span className="lp-fundacoes-rotulo">Construído sobre</span>
        {FUNDACOES.map((f) => (
          <span key={f} className="lp-fundacao">
            {f}
          </span>
        ))}
      </div>
    </Faixa>
  )
}

function StatsFaixa() {
  return (
    <Faixa espaco={88}>
      <div className="lp-stats">
        {STATS.map((s) => (
          <div key={s.rotulo}>
            <div className="lp-stat-valor">{s.valor}</div>
            <div className="lp-stat-rotulo">{s.rotulo}</div>
          </div>
        ))}
      </div>
    </Faixa>
  )
}

function FalhasFaixa() {
  return (
    <Faixa>
      <span className="lp-campo">O que a planilha perde</span>
      <h2 className="lp-h2" style={{ maxWidth: '22ch' }}>
        Planilha não avisa quando a revisão passou do ponto
      </h2>
      <div className="lp-grade lp-grade-2" style={{ marginTop: 40 }}>
        {DORES.map((d) => (
          <div key={d.num} className="lp-cartao">
            <span className="lp-dor-num">{d.num}</span>
            <p className="lp-dor-texto">{d.texto}</p>
          </div>
        ))}
      </div>
    </Faixa>
  )
}

function ComparativoFaixa() {
  return (
    <Faixa>
      <span className="lp-campo">Planilha × Frota360</span>
      <h2 className="lp-h2" style={{ maxWidth: '20ch' }}>
        O que muda quando sai da planilha
      </h2>
      <Rolagem rotulo="Comparativo entre a planilha e o Frota360">
        <div className="lp-compara" style={{ marginTop: 40 }}>
          <div className="lp-compara-tabela">
            <div className="lp-compara-linha lp-compara-cab">
              <div>Dado</div>
              <div>Na planilha</div>
              <div className="is-app">No Frota360</div>
            </div>
            {COMPARATIVO.map((c) => (
              <div key={c.item} className="lp-compara-linha">
                <div className="lp-compara-item">{c.item}</div>
                <div className="lp-compara-antes">{c.planilha}</div>
                <div className="lp-compara-depois">{c.app}</div>
              </div>
            ))}
          </div>
        </div>
      </Rolagem>
    </Faixa>
  )
}

function RecursosFaixa() {
  return (
    <Faixa id="recursos">
      <span className="lp-campo lp-campo-acento">Recursos</span>
      <h2 className="lp-h2" style={{ maxWidth: '24ch' }}>
        Quatro cadastros, uma operação inteira sob controle
      </h2>
      <div className="lp-grade lp-grade-2" style={{ marginTop: 40 }}>
        {RECURSOS.map((r) => (
          <div key={r.titulo} className="lp-cartao lp-recurso">
            <div className="lp-recurso-marca">{r.inicial}</div>
            <h3 className="lp-h3">{r.titulo}</h3>
            <p className="lp-recurso-texto">{r.texto}</p>
            <ul className="lp-lista">
              {r.itens.map((it) => (
                <li key={it}>
                  <CheckIcon size={15} />
                  <span>{it}</span>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </Faixa>
  )
}

function ComoFuncionaFaixa() {
  return (
    <Faixa id="como-funciona">
      <span className="lp-campo lp-campo-acento">Como funciona</span>
      <h2 className="lp-h2" style={{ maxWidth: '24ch' }}>
        Do primeiro contato ao painel rodando em uma semana
      </h2>
      <div className="lp-grade lp-grade-3" style={{ marginTop: 40 }}>
        {PASSOS.map((p) => (
          <div key={p.num} className="lp-passo">
            <span className="lp-passo-num">{p.num}</span>
            <h3 className="lp-h3">{p.titulo}</h3>
            <p>{p.texto}</p>
          </div>
        ))}
      </div>
    </Faixa>
  )
}

function RotasFaixa() {
  return (
    <Faixa espaco={96}>
      <div className="lp-split">
        <div>
          <span className="lp-campo lp-campo-acento">Rotas</span>
          <h2 className="lp-h2-sm" style={{ maxWidth: '18ch' }}>
            A viagem de hoje e o histórico da semana na mesma tela
          </h2>
          <p className="lp-lead">
            Qualquer pessoa da equipe pode lançar uma rota. Encerrar é um clique — e o histórico fica preso ao
            motorista e ao veículo.
          </p>
        </div>
        <Dispositivo>
          <div>
            <div className="lp-painel-cab">
              <span className="lp-painel-titulo">Rotas</span>
              <span className="lp-contagem">2 em curso</span>
              <div className="lp-painel-direita">
                <span className="btn btn-primary" aria-hidden="true">
                  Nova rota
                </span>
              </div>
            </div>
            <Rolagem rotulo="Exemplo da lista de rotas">
              <table className="table">
                <thead>
                  <tr>
                    <th>Trecho</th>
                    <th>Motorista</th>
                    <th>Veículo</th>
                    <th>Situação</th>
                  </tr>
                </thead>
                <tbody>
                  {ROTAS_MOCK.map((r) => (
                    <tr key={r.trecho}>
                      <td>
                        <span className="lp-painel-forte">{r.trecho}</span>
                        <div className="lp-painel-sub">Início {r.inicio}</div>
                      </td>
                      <td>{r.motorista}</td>
                      <td>{r.veiculo}</td>
                      <td>
                        <span className={CLASSE_ETIQ[r.tom]}>{r.situacao}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </Rolagem>
          </div>
        </Dispositivo>
      </div>
    </Faixa>
  )
}

function ManutencaoFaixa() {
  return (
    <Faixa id="manutencao">
      <span className="lp-campo lp-campo-acento">Manutenção</span>
      <h2 className="lp-h2" style={{ maxWidth: '22ch' }}>
        O que está vencendo sobe para o topo
      </h2>
      <p className="lp-lead">
        O painel compara a quilometragem prevista de cada manutenção com a km atual do veículo. Quando concluir, você
        informa a km real e o custo — e a quilometragem do veículo sobe junto, sem atualizar em dois lugares.
      </p>

      <div className="lp-split-manutencao" style={{ marginTop: 40 }}>
        <Dispositivo>
          <div>
            <div className="lp-painel-cab">
              <span className="lp-painel-titulo">Manutenções</span>
              <span className="lp-contagem">4 em aberto</span>
              <div className="lp-painel-direita">
                <span className="btn btn-primary" aria-hidden="true">
                  Nova manutenção
                </span>
              </div>
            </div>
            <Rolagem rotulo="Exemplo da lista de manutenções">
              <table className="table">
                <thead>
                  <tr>
                    <th>Veículo</th>
                    <th>Tipo</th>
                    <th className="lp-painel-numero-dir">Prevista</th>
                    <th>Andamento</th>
                    <th>Situação</th>
                  </tr>
                </thead>
                <tbody>
                  {MANUTENCOES_MOCK.map((m) => (
                    <tr key={`${m.placa}-${m.tipo}`}>
                      <td>
                        <span className="lp-painel-forte">{m.placa}</span>
                        <div className="lp-painel-sub">{m.modelo}</div>
                      </td>
                      <td>{m.tipo}</td>
                      <td className="lp-painel-numero lp-painel-numero-dir">{m.km}</td>
                      <td className="lp-painel-numero">{m.andamento}</td>
                      <td>
                        <span className={CLASSE_ETIQ[m.tom]}>{m.situacao}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </Rolagem>
          </div>
        </Dispositivo>

        <div className="lp-manutencao-lateral">
          <div className="lp-cartao">
            <div className="lp-tipos-titulo">Tipos de manutenção</div>
            <p className="lp-tipos-nota">Catálogo da empresa, com intervalo em km.</p>
            <div>
              {TIPOS_MOCK.map((t) => (
                <div key={t.nome} className="lp-tipo-linha">
                  <span>{t.nome}</span>
                  <span className="lp-tipo-intervalo">{t.intervalo}</span>
                </div>
              ))}
            </div>
          </div>
          <div className="lp-cartao">
            <div className="lp-destaque-kicker">Concluir em um clique</div>
            <p className="lp-destaque-texto">
              Ao concluir, você informa a km real e o custo — e a quilometragem do veículo já sobe junto. Nada de
              atualizar em dois lugares.
            </p>
          </div>
        </div>
      </div>
    </Faixa>
  )
}

function PermissoesFaixa() {
  return (
    <Faixa id="permissoes">
      <div className="lp-split">
        <div>
          <span className="lp-campo lp-campo-acento">Permissões</span>
          <h2 className="lp-h2-sm" style={{ maxWidth: '20ch' }}>
            Cada pessoa vê e faz exatamente o que deve
          </h2>
          <p className="lp-lead">
            Três papéis prontos, sem tela de configuração complicada. Quem entra por convite já chega com o papel
            certo.
          </p>
        </div>
        <Rolagem rotulo="Matriz de permissões por papel">
          <div className="lp-matriz-cartao">
            <table className="lp-matriz">
              <thead>
                <tr>
                  <th>Ação</th>
                  <th>Admin</th>
                  <th>Supervisor</th>
                  <th>Operador</th>
                </tr>
              </thead>
              <tbody>
                {MATRIZ.map((m) => (
                  <tr key={m.acao}>
                    <td>{m.acao}</td>
                    {/* O ícone é visual; a palavra continua no DOM para quem
                        usa leitor de tela — um ✓ sozinho não se lê. Mapeia pelo nome do
                        papel (a mesma ordem do <thead> acima), não por posição. */}
                    {PAPEIS_DA_MATRIZ.map((papel) => (
                      <td key={papel} className={m[papel] ? 'lp-sim' : 'lp-nao'}>
                        {m[papel] ? <CheckIcon size={17} /> : <XIcon size={16} />}
                        <span className="lp-oculto">{m[papel] ? 'Sim' : 'Não'}</span>
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Rolagem>
      </div>
    </Faixa>
  )
}

function SegurancaFaixa() {
  return (
    <Faixa>
      <div className="lp-grade lp-grade-3">
        {SEGURANCA.map((g) => (
          <div key={g.titulo} className="lp-cartao lp-seguranca">
            <div className="lp-seguranca-kicker">{g.kicker}</div>
            <h3>{g.titulo}</h3>
            <p>{g.texto}</p>
          </div>
        ))}
      </div>
    </Faixa>
  )
}

function ObjecoesFaixa() {
  return (
    <Faixa>
      <h2 className="lp-h2-sm" style={{ maxWidth: '24ch' }}>
        O que a gente mais escuta na primeira conversa
      </h2>
      <div className="lp-grade lp-grade-3" style={{ marginTop: 40 }}>
        {OBJECOES.map((o) => (
          <div key={o.q}>
            <p className="lp-objecao-q">{o.q}</p>
            <p className="lp-objecao-r">{o.r}</p>
          </div>
        ))}
      </div>
    </Faixa>
  )
}

function DuvidasFaixa() {
  return (
    <Faixa id="duvidas" estreito>
      <span className="lp-campo">Dúvidas</span>
      <h2 className="lp-h2-sm">O que a gente mais escuta na primeira conversa</h2>
      <div style={{ marginTop: 32 }}>
        {DUVIDAS.map((d) => (
          <details key={d.p} className="lp-faq">
            <summary>
              {d.p}
              <span className="lp-faq-sinal" aria-hidden="true">
                +
              </span>
            </summary>
            <p className="lp-faq-resposta">{d.r}</p>
          </details>
        ))}
      </div>
    </Faixa>
  )
}

function CtaSection() {
  return (
    <Faixa id="demonstracao">
      <div className="lp-cta">
        <div className="lp-cta-grade">
          <div>
            <span className="lp-campo">Demonstração</span>
            <h2 className="lp-h2-sm">Mostre sua frota. A gente mostra o painel.</h2>
            <p className="lp-cta-sub">
              Uma conversa de 20 minutos: você conta como controla a frota hoje e sai com a empresa já configurada
              para testar.
            </p>
            <div className="lp-cta-acoes">
              <a href={LINK_WHATS} className="lp-btn lp-btn-claro lp-btn-g">
                <WhatsappIcon size={17} />
                Prefiro o WhatsApp
              </a>
              <a href={LINK_EMAIL} className="lp-cta-mail">
                {EMAIL}
              </a>
            </div>
          </div>
          <FormularioDemonstracao />
        </div>
      </div>
    </Faixa>
  )
}

function RodapeLanding() {
  return (
    <footer className="lp-rodape">
      <Link to="/" className="lp-brand" aria-label="Frota 360 — início">
        <Marca tamanho={18} />
      </Link>
      <Link to="/login">Entrar</Link>
      <a href={LINK_EMAIL}>{EMAIL}</a>
      <span>© {new Date().getFullYear()} Frota 360</span>
    </footer>
  )
}

// ── página ──────────────────────────────────────────────────────────────────

export function LandingPage() {
  const raizRef = useRef<HTMLDivElement>(null)
  useRevelarFaixas(raizRef)

  return (
    <div className="lp" ref={raizRef}>
      <CabecalhoLanding />
      <HeroLanding />
      <PainelMockFaixa />
      <FundacoesFaixa />
      <StatsFaixa />
      <FalhasFaixa />
      <ComparativoFaixa />
      <RecursosFaixa />
      <ComoFuncionaFaixa />
      <RotasFaixa />
      <ManutencaoFaixa />
      <PermissoesFaixa />
      <SegurancaFaixa />
      <ObjecoesFaixa />
      <DuvidasFaixa />
      <CtaSection />
      <RodapeLanding />
    </div>
  )
}

const FORM_VAZIO = { nome: '', empresa: '', email: '', frota: '' }

/**
 * Pedido de demonstração. O envio ainda é só de tela: não existe endpoint
 * público na API (§6 do CONTEXTO — não há cadastro aberto), então os dados são
 * abertos no cliente de e-mail do visitante. Como não dá para saber se o
 * cliente de e-mail abriu, a confirmação não afirma que abriu — ela diz o que
 * era para acontecer e oferece a saída.
 */
function FormularioDemonstracao() {
  const [form, setForm] = useState(FORM_VAZIO)
  const [enviado, setEnviado] = useState(false)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const corpo = [
      `Nome: ${form.nome}`,
      `Empresa: ${form.empresa}`,
      `E-mail: ${form.email}`,
      `Tamanho da frota: ${form.frota}`,
    ].join('\n')
    window.location.href = `${LINK_EMAIL}?subject=${encodeURIComponent(
      'Pedido de demonstração — Frota 360',
    )}&body=${encodeURIComponent(corpo)}`
    setEnviado(true)
  }

  return (
    <form className="lp-form" onSubmit={handleSubmit}>
      <div className="lp-form-titulo">Pedir uma demonstração</div>
      {enviado ? (
        <p className="lp-form-ok" style={{ marginTop: 18 }}>
          Seu programa de e-mail deve ter aberto com os dados preenchidos — é só enviar. Se nada abriu, escreva para{' '}
          <a href={LINK_EMAIL}>{EMAIL}</a> ou <a href={LINK_WHATS}>chame no WhatsApp</a>. A gente responde no mesmo dia
          útil.
        </p>
      ) : (
        <div className="lp-form-campos">
          <input
            className="lp-input"
            type="text"
            required
            placeholder="Seu nome"
            aria-label="Seu nome"
            autoComplete="name"
            value={form.nome}
            onChange={(e) => setForm({ ...form, nome: e.target.value })}
          />
          <input
            className="lp-input"
            type="text"
            required
            placeholder="Empresa"
            aria-label="Empresa"
            autoComplete="organization"
            value={form.empresa}
            onChange={(e) => setForm({ ...form, empresa: e.target.value })}
          />
          <input
            className="lp-input"
            type="email"
            required
            placeholder="E-mail de trabalho"
            aria-label="E-mail de trabalho"
            autoComplete="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
          />
          <select
            className="lp-input"
            required
            aria-label="Tamanho da frota"
            value={form.frota}
            onChange={(e) => setForm({ ...form, frota: e.target.value })}
          >
            <option value="">Tamanho da frota</option>
            {TAMANHOS_FROTA.map((t) => (
              <option key={t}>{t}</option>
            ))}
          </select>
          <button type="submit" className="lp-btn lp-btn-primario lp-btn-block" style={{ padding: 14 }}>
            Quero ver o painel
          </button>
          <p className="lp-form-nota">Sem compromisso. Usamos seus dados apenas para entrar em contato.</p>
        </div>
      )}
    </form>
  )
}
