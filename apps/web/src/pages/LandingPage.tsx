import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { LogoMark, Wordmark } from '../components/Logo'
import { CheckIcon, SearchIcon, WhatsappIcon } from '../components/icons'
import '../styles/landing.css'

const WHATSAPP = '5547991120404'
const EMAIL = 'phsvscheidt2003@gmail.com'
const LINK_WHATS = `https://wa.me/${WHATSAPP}`
const LINK_EMAIL = `mailto:${EMAIL}`

// ── conteúdo ────────────────────────────────────────────────────────────────

const MENU_MOCK = ['Visão geral', 'Motoristas', 'Veículos', 'Rotas', 'Manutenções', 'Usuários']
const MENU_ATIVO = 'Veículos'

type Tom = 'accent' | 'neutral' | 'warn'

const VEICULOS_MOCK: { placa: string; modelo: string; km: string; motorista: string; situacao: string; tom: Tom }[] = [
  { placa: 'MHT-4G21', modelo: 'Ford Cargo 816', km: '148.230 km', motorista: 'Ana Ribeiro', situacao: 'Em rota', tom: 'accent' },
  { placa: 'LZP-9D14', modelo: 'VW Constellation', km: '312.940 km', motorista: 'Ivo Nascimento', situacao: 'Em rota', tom: 'accent' },
  { placa: 'QKA-7B08', modelo: 'VW Delivery', km: '96.510 km', motorista: 'Carlos Deppe', situacao: 'Disponível', tom: 'neutral' },
  { placa: 'RTB-2C55', modelo: 'Mercedes Sprinter', km: '74.180 km', motorista: 'Marta Lins', situacao: 'Disponível', tom: 'neutral' },
  { placa: 'MJU-5F71', modelo: 'VW Saveiro', km: '52.700 km', motorista: 'Helena Cruz', situacao: 'Revisão', tom: 'warn' },
  { placa: 'PGE-1H47', modelo: 'Fiat Fiorino', km: '38.420 km', motorista: 'Régis Alves', situacao: 'Disponível', tom: 'neutral' },
]

const FUNDACOES = [
  'Multiempresa desde o primeiro dia',
  'Convites com papel definido',
  'Sessão revogada na hora',
  'Manutenção preventiva por km',
  'Validação de CPF e idade',
]

const STATS = [
  { valor: '5–500', rotulo: 'Veículos por empresa, sem mudar de plano' },
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
  { trecho: 'Joinville → Curitiba', motorista: 'Ana Ribeiro', veiculo: 'MHT-4G21', inicio: '22/08 06:10', situacao: 'Em curso', tom: 'accent' },
  { trecho: 'Pátio → Blumenau', motorista: 'Carlos Deppe', veiculo: 'QKA-7B08', inicio: '22/08 07:40', situacao: 'Em curso', tom: 'accent' },
  { trecho: 'CD Norte → Itajaí', motorista: 'Marta Lins', veiculo: 'RTB-2C55', inicio: '21/08 13:05', situacao: 'Encerrada', tom: 'neutral' },
  { trecho: 'Joinville → Porto Itapoá', motorista: 'Ivo Nascimento', veiculo: 'LZP-9D14', inicio: '21/08 05:30', situacao: 'Encerrada', tom: 'neutral' },
  { trecho: 'São Bento → Pátio', motorista: 'Helena Cruz', veiculo: 'MJU-5F71', inicio: '20/08 16:20', situacao: 'Encerrada', tom: 'neutral' },
]

const MANUTENCOES_MOCK: {
  placa: string
  modelo: string
  tipo: string
  km: string
  andamento: string
  custo: string
  situacao: string
  tom: Tom
}[] = [
  { placa: 'QRS5T90', modelo: 'HB20 · 150 km', tipo: 'Revisão', km: '10.150 km', andamento: 'faltam 10.000 km', custo: '—', situacao: 'Pendente', tom: 'accent' },
  { placa: 'LZP-9D14', modelo: 'Constellation · 312.940 km', tipo: 'Troca de pneus', km: '315.000 km', andamento: 'faltam 2.060 km', custo: '—', situacao: 'Pendente', tom: 'accent' },
  { placa: 'MJU-5F71', modelo: 'Saveiro · 52.700 km', tipo: 'Troca de óleo', km: '52.000 km', andamento: 'atrasada 700 km', custo: '—', situacao: 'Atrasada', tom: 'warn' },
  { placa: 'ABC1D23', modelo: 'Civic 2.0 · 65.200 km', tipo: 'Troca de óleo', km: '55.200 km', andamento: '25/08/2026 · 65.200 km', custo: 'R$ 500,00', situacao: 'Concluída', tom: 'neutral' },
]

const TIPOS_MOCK = [
  { nome: 'Troca de óleo', intervalo: '10.000 km' },
  { nome: 'Revisão', intervalo: '10.000 km' },
  { nome: 'Filtro de ar-condicionado', intervalo: '50.000 km' },
]

const MATRIZ = [
  { acao: 'Ver tudo da empresa', admin: 'Sim', supervisor: 'Sim', operador: 'Sim' },
  { acao: 'Lançar e editar rotas', admin: 'Sim', supervisor: 'Sim', operador: 'Sim' },
  { acao: 'Cadastrar motoristas e veículos', admin: 'Sim', supervisor: 'Sim', operador: '—' },
  { acao: 'Agendar e concluir manutenções', admin: 'Sim', supervisor: 'Sim', operador: '—' },
  { acao: 'Excluir qualquer registro', admin: 'Sim', supervisor: '—', operador: '—' },
  { acao: 'Convidar e gerenciar usuários', admin: 'Sim', supervisor: '—', operador: '—' },
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

const FAQ = [
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

// ── peças ───────────────────────────────────────────────────────────────────

const CLASSE_TAG: Record<Tom, string> = {
  accent: 'lp-tag lp-tag-accent',
  neutral: 'lp-tag lp-tag-neutral',
  warn: 'lp-tag lp-tag-warn',
}

function Marca({ tamanho = 22 }: { tamanho?: number }) {
  return (
    <>
      <LogoMark size={tamanho} />
      <Wordmark size={16} />
    </>
  )
}

function Secao({
  id,
  className = 'lp-wrap lp-section',
  children,
}: {
  id?: string
  className?: string
  children: ReactNode
}) {
  return (
    <section id={id} className={`${className} lp-reveal`} data-reveal style={id ? { scrollMarginTop: 96 } : undefined}>
      {children}
    </section>
  )
}

// ── página ──────────────────────────────────────────────────────────────────

export function LandingPage() {
  const raiz = useRef<HTMLDivElement>(null)

  // Revela as seções conforme entram na tela. Sem IntersectionObserver, tudo
  // aparece de uma vez — o conteúdo nunca pode ficar preso invisível.
  useEffect(() => {
    const alvos = Array.from(raiz.current?.querySelectorAll('[data-reveal]') ?? [])
    if (alvos.length === 0) return

    if (!('IntersectionObserver' in window)) {
      alvos.forEach((el) => el.classList.add('is-visible'))
      return
    }

    const observador = new IntersectionObserver(
      (entradas) => {
        entradas.forEach((entrada) => {
          if (!entrada.isIntersecting) return
          entrada.target.classList.add('is-visible')
          observador.unobserve(entrada.target)
        })
      },
      { rootMargin: '0px 0px -12% 0px' },
    )
    alvos.forEach((el) => observador.observe(el))
    return () => observador.disconnect()
  }, [])

  return (
    <div className="lp" ref={raiz}>
      <header className="lp-nav">
        <Link to="/" className="lp-brand" aria-label="Frota 360 — início">
          <Marca />
        </Link>
        <nav className="lp-nav-links">
          <a href="#recursos">Recursos</a>
          <a href="#manutencao">Manutenção</a>
          <a href="#como-funciona">Como funciona</a>
          <a href="#permissoes">Permissões</a>
          <a href="#faq">Dúvidas</a>
        </nav>
        <div className="lp-nav-actions">
          <Link to="/login" className="lp-btn lp-btn-quiet lp-btn-nav">
            Entrar
          </Link>
          <a href={LINK_WHATS} className="lp-btn lp-btn-primary lp-btn-sm">
            Falar com a gente
          </a>
        </div>
      </header>

      <section className="lp-hero">
        <h1 className="lp-h1" style={{ marginTop: 26, maxWidth: '20ch' }}>
          Sua frota inteira em um único painel
        </h1>
        <p className="lp-hero-sub">
          Motoristas, veículos, manutenções e rotas em um só lugar  com quilometragem em dia, histórico de viagens e controle de
          quem pode fazer o quê.
        </p>
        <div className="lp-hero-ctas">
          <a href={LINK_WHATS} className="lp-btn lp-btn-primary lp-btn-lg">
            <WhatsappIcon size={17} />
            Falar no WhatsApp
          </a>
          <a href="#demonstracao" className="lp-btn lp-btn-outline lp-btn-lg">
            Pedir uma demonstração
          </a>
        </div>
        <p className="lp-fineprint">
          Implantação assistida · sem cartão de crédito · <Link to="/login">já é cliente?</Link>
        </p>
      </section>

      {/* Mock do painel: dados ilustrativos, não vêm da API. */}
      <section className="lp-wrap">
        <div className="lp-mock">
          <aside className="lp-mock-aside">
            <div className="lp-brand" style={{ padding: '0 8px', marginRight: 0 }}>
              <Marca tamanho={20} />
            </div>
            <nav style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
              {MENU_MOCK.map((item) => (
                <div key={item} className={`lp-mock-item${item === MENU_ATIVO ? ' is-active' : ''}`}>
                  <span className="lp-mock-square" />
                  {item}
                </div>
              ))}
            </nav>
            <div className="lp-mock-user">
              <span className="lp-avatar">PD</span>
              <div style={{ fontSize: 12, lineHeight: 1.3 }}>
                Paulo D.
                <div style={{ color: 'var(--lp-ink-45)' }}>Admin</div>
              </div>
            </div>
          </aside>

          <div>
            <div className="lp-mock-head">
              <span className="lp-mock-title">Veículos</span>
              <span className="lp-pill-count">42 cadastrados</span>
              <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 10 }}>
                <span className="lp-mock-search">
                  <SearchIcon size={14} />
                  Buscar placa ou modelo
                </span>
                <span className="lp-mock-btn">Novo veículo</span>
              </div>
            </div>
            <div className="lp-scroll">
              <table className="lp-table">
                <thead>
                  <tr>
                    <th>Placa</th>
                    <th>Modelo</th>
                    <th className="num">Quilometragem</th>
                    <th>Último motorista</th>
                    <th>Situação</th>
                  </tr>
                </thead>
                <tbody>
                  {VEICULOS_MOCK.map((v) => (
                    <tr key={v.placa}>
                      <td className="lp-strong">{v.placa}</td>
                      <td>{v.modelo}</td>
                      <td className="num">{v.km}</td>
                      <td>{v.motorista}</td>
                      <td>
                        <span className={CLASSE_TAG[v.tom]}>{v.situacao}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="lp-mock-foot">
              <span className="lp-dot" />
              Última atualização hoje, 14:20 — por Ana Ribeiro
            </div>
          </div>
        </div>
      </section>

      <Secao className="lp-wrap">
        <div className="lp-chips">
          <span style={{ fontSize: 13, color: 'var(--lp-ink-45)' }}>Construído sobre</span>
          {FUNDACOES.map((f) => (
            <span key={f} className="lp-chip">
              {f}
            </span>
          ))}
        </div>
      </Secao>

      <Secao className="lp-wrap lp-section-tight">
        <div className="lp-grid lp-grid-4">
          {STATS.map((s) => (
            <div key={s.rotulo}>
              <div className="lp-stat-valor">{s.valor}</div>
              <div className="lp-stat-rotulo">{s.rotulo}</div>
            </div>
          ))}
        </div>
      </Secao>

      <Secao>
        <h2 className="lp-h2" style={{ maxWidth: '24ch' }}>
          Planilha não avisa quando a revisão passou do ponto
        </h2>
        <div className="lp-grid lp-grid-2" style={{ marginTop: 44, gap: 28 }}>
          {DORES.map((d) => (
            <div key={d.num} className="lp-card lp-card-lift lp-card-pad" style={{ borderRadius: 16 }}>
              <div className="lp-num">{d.num}</div>
              <p style={{ fontSize: 16.5, color: 'var(--lp-ink-80)', marginTop: 10 }}>{d.texto}</p>
            </div>
          ))}
        </div>
      </Secao>

      <Secao>
        <h2 className="lp-h2" style={{ maxWidth: '22ch' }}>
          O que muda quando sai da planilha
        </h2>
        <div className="lp-card lp-card-float" style={{ marginTop: 44 }}>
          <div className="lp-scroll">
            <div className="lp-compare">
              <div className="lp-compare-row lp-compare-head">
                <div />
                <div>Na planilha</div>
                <div className="is-app">No Frota360</div>
              </div>
              {COMPARATIVO.map((c) => (
                <div key={c.item} className="lp-compare-row">
                  <div style={{ fontSize: 15, fontWeight: 600 }}>{c.item}</div>
                  <div style={{ fontSize: 14.5, color: 'rgba(27,29,33,.5)' }}>{c.planilha}</div>
                  <div style={{ fontSize: 14.5, color: 'var(--lp-ink-80)' }}>{c.app}</div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </Secao>

      <Secao id="recursos">
        <div className="lp-kicker">Recursos</div>
        <h2 className="lp-h2" style={{ maxWidth: '26ch', marginTop: 14 }}>
          Quatro cadastros, uma operação inteira sob controle
        </h2>
        <div className="lp-grid lp-grid-2" style={{ marginTop: 48 }}>
          {RECURSOS.map((f) => (
            <div key={f.titulo} className="lp-feature">
              <div className="lp-feature-mark">{f.inicial}</div>
              <h3 style={{ fontSize: 22 }}>{f.titulo}</h3>
              <p style={{ fontSize: 15.5, color: 'var(--lp-ink-60)' }}>{f.texto}</p>
              <ul className="lp-check-list">
                {f.itens.map((it) => (
                  <li key={it}>
                    <CheckIcon size={15} />
                    <span>{it}</span>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      </Secao>

      <Secao id="como-funciona">
        <div className="lp-kicker">Como funciona</div>
        <h2 className="lp-h2" style={{ maxWidth: '24ch', marginTop: 14 }}>
          Do primeiro contato ao painel rodando em uma semana
        </h2>
        <div className="lp-grid lp-grid-3" style={{ marginTop: 48, gap: 36 }}>
          {PASSOS.map((p) => (
            <div key={p.num} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <div className="lp-step-num">{p.num}</div>
              <h3 style={{ fontSize: 20, marginTop: 4 }}>{p.titulo}</h3>
              <p style={{ fontSize: 15.5, color: 'var(--lp-ink-60)' }}>{p.texto}</p>
            </div>
          ))}
        </div>
      </Secao>

      <Secao className="lp-wrap lp-section-tight lp-split lp-split-rotas">
        <div>
          <div className="lp-kicker">Rotas</div>
          <h2 className="lp-h3" style={{ maxWidth: '18ch', marginTop: 14 }}>
            A viagem de hoje e o histórico da semana na mesma tela
          </h2>
          <p className="lp-lead" style={{ fontSize: 16, maxWidth: '38ch' }}>
            Qualquer pessoa da equipe pode lançar uma rota. Encerrar é um clique — e o histórico fica preso ao motorista
            e ao veículo.
          </p>
        </div>
        <div className="lp-card lp-card-float">
          <div className="lp-list-head">
            <span style={{ fontSize: 15, fontWeight: 600 }}>Rotas</span>
            <span className="lp-pill-count">2 em curso</span>
            <span className="lp-mock-btn" style={{ marginLeft: 'auto' }}>
              Nova rota
            </span>
          </div>
          <div className="lp-scroll">
            {ROTAS_MOCK.map((r) => (
              <div key={r.trecho} className="lp-rota-row">
                <div style={{ fontSize: 14, fontWeight: 600 }}>
                  {r.trecho}
                  <div className="lp-sub" style={{ fontWeight: 400 }}>
                    Início {r.inicio}
                  </div>
                </div>
                <div style={{ fontSize: 13.5, color: 'rgba(27,29,33,.65)' }}>{r.motorista}</div>
                <div style={{ fontSize: 13.5, color: 'rgba(27,29,33,.5)' }}>{r.veiculo}</div>
                <div>
                  <span className={CLASSE_TAG[r.tom]}>{r.situacao}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </Secao>

      <Secao id="manutencao">
        <div className="lp-kicker">Manutenção preventiva</div>
        <h2 className="lp-h2" style={{ maxWidth: '24ch', marginTop: 14 }}>
          A revisão avisa antes de virar oficina
        </h2>
        <p className="lp-lead" style={{ maxWidth: '56ch' }}>
          Você agenda a manutenção na quilometragem prevista. Conforme o veículo roda, o painel recalcula sozinho
          quantos quilômetros faltam — e joga o que está vencendo para o topo da lista.
        </p>
        <div className="lp-grid lp-split-manutencao" style={{ marginTop: 44 }}>
          <div className="lp-card lp-card-float">
            <div className="lp-list-head">
              <span style={{ fontSize: 15, fontWeight: 600 }}>Manutenções</span>
              <span className="lp-pill-count">3 pendentes</span>
              <span className="lp-mock-btn" style={{ marginLeft: 'auto' }}>
                Nova manutenção
              </span>
            </div>
            <div className="lp-scroll">
              <table className="lp-table">
                <thead>
                  <tr>
                    <th>Veículo</th>
                    <th>Tipo</th>
                    <th>Prevista</th>
                    <th>Andamento</th>
                    <th>Situação</th>
                  </tr>
                </thead>
                <tbody>
                  {MANUTENCOES_MOCK.map((m) => (
                    <tr key={`${m.placa}-${m.tipo}`}>
                      <td>
                        <span className="lp-strong">{m.placa}</span>
                        <div className="lp-sub">{m.modelo}</div>
                      </td>
                      <td>{m.tipo}</td>
                      <td style={{ fontVariantNumeric: 'tabular-nums' }}>{m.km}</td>
                      <td style={{ color: 'rgba(27,29,33,.55)' }}>
                        {m.andamento}
                        <div className="lp-sub" style={{ color: 'rgba(27,29,33,.4)' }}>
                          {m.custo}
                        </div>
                      </td>
                      <td>
                        <span className={CLASSE_TAG[m.tom]}>{m.situacao}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
            <div className="lp-card lp-card-pad-sm" style={{ borderRadius: 16 }}>
              <div style={{ fontSize: 15, fontWeight: 600 }}>Tipos de manutenção</div>
              <p style={{ fontSize: 13.5, color: 'rgba(27,29,33,.5)', marginTop: 4 }}>
                Catálogo da empresa, com intervalo em km.
              </p>
              <div style={{ marginTop: 14 }}>
                {TIPOS_MOCK.map((t) => (
                  <div key={t.nome} className="lp-tipo-row">
                    <span>{t.nome}</span>
                    <span>{t.intervalo}</span>
                  </div>
                ))}
              </div>
            </div>
            <div className="lp-card lp-card-lift lp-card-pad-sm" style={{ borderRadius: 16 }}>
              <div className="lp-num">Concluir em um clique</div>
              <p style={{ fontSize: 14.5, color: 'rgba(27,29,33,.65)', marginTop: 10 }}>
                Ao concluir, você informa a km real e o custo — e a quilometragem do veículo já sobe junto. Nada de
                atualizar em dois lugares.
              </p>
            </div>
          </div>
        </div>
      </Secao>

      <Secao id="permissoes" className="lp-wrap lp-section lp-split">
        <div>
          <div className="lp-kicker">Permissões</div>
          <h2 className="lp-h2-sm" style={{ maxWidth: '20ch', marginTop: 14 }}>
            Cada pessoa vê e faz exatamente o que deve
          </h2>
          <p className="lp-lead" style={{ maxWidth: '42ch', marginTop: 20 }}>
            Três papéis prontos, sem tela de configuração complicada. Quem entra por convite já chega com o papel certo.
          </p>
        </div>
        <div className="lp-matriz">
          <div className="lp-scroll">
            <table>
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
                    <td>{m.admin}</td>
                    <td>{m.supervisor}</td>
                    <td>{m.operador}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </Secao>

      <Secao>
        <div className="lp-grid lp-grid-3">
          {SEGURANCA.map((g) => (
            <div key={g.titulo} className="lp-card lp-card-lift lp-card-pad">
              <div className="lp-num">{g.kicker}</div>
              <h3 style={{ fontSize: 19, marginTop: 12 }}>{g.titulo}</h3>
              <p style={{ fontSize: 15.5, color: 'var(--lp-ink-60)', marginTop: 8 }}>{g.texto}</p>
            </div>
          ))}
        </div>
      </Secao>

      <Secao>
        <h2 className="lp-h2-sm" style={{ maxWidth: '24ch' }}>
          O que a gente mais escuta na primeira conversa
        </h2>
        <div className="lp-grid lp-grid-3" style={{ marginTop: 44 }}>
          {OBJECOES.map((o) => (
            <div key={o.q} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <p className="lp-objecao-q">{o.q}</p>
              <p style={{ fontSize: 15.5, color: 'var(--lp-ink-60)' }}>{o.r}</p>
            </div>
          ))}
        </div>
      </Secao>

      <Secao id="faq" className="lp-wrap-narrow lp-section">
        <h2 className="lp-h2-sm">Dúvidas frequentes</h2>
        <div style={{ marginTop: 36 }}>
          {FAQ.map((q) => (
            <div key={q.p} className="lp-faq-item">
              <h3>{q.p}</h3>
              <p>{q.r}</p>
            </div>
          ))}
        </div>
      </Secao>

      <Secao id="demonstracao">
        <div className="lp-cta">
          <div>
            <h2 className="lp-h2">Mostre sua frota. A gente mostra o painel.</h2>
            <p className="lp-cta-sub">
              Uma conversa de 20 minutos: você conta como controla a frota hoje e sai com a empresa já configurada para
              testar.
            </p>
            <div className="lp-cta-actions">
              <a href={LINK_WHATS} className="lp-btn lp-btn-onblue lp-btn-md">
                Prefiro o WhatsApp
              </a>
              <a href={LINK_EMAIL} className="lp-cta-mail">
                {EMAIL}
              </a>
            </div>
          </div>
          <FormularioDemonstracao />
        </div>
      </Secao>

      <footer className="lp-footer">
        <Link to="/" className="lp-brand" aria-label="Frota 360 — início">
          <Marca tamanho={18} />
        </Link>
        <Link to="/login">Entrar</Link>
        <a href={LINK_EMAIL}>{EMAIL}</a>
        <span>© 2026 Frota 360</span>
      </footer>
    </div>
  )
}

const FORM_VAZIO = { nome: '', empresa: '', email: '', frota: '' }

/**
 * Pedido de demonstração. O envio ainda é só de tela: não existe endpoint
 * público na API (§6 do CONTEXTO — não há cadastro aberto), então os dados
 * são abertos no cliente de e-mail do visitante.
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
      <div className="lp-form-title">Pedir uma demonstração</div>
      {enviado ? (
        <p className="lp-form-ok">
          Abrimos seu e-mail com os dados preenchidos — é só enviar. A gente responde no mesmo dia útil; se preferir
          adiantar, chame no WhatsApp.
        </p>
      ) : (
        <div className="lp-form-fields">
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
          <button type="submit" className="lp-btn lp-btn-primary" style={{ padding: 14, fontSize: 15 }}>
            Quero ver o painel
          </button>
          <p className="lp-form-note">Sem compromisso. Usamos seus dados apenas para entrar em contato.</p>
        </div>
      )}
    </form>
  )
}
