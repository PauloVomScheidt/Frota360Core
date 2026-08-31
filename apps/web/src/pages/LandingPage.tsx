import { useEffect, useRef, useState, type CSSProperties, type FormEvent, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { LogoMark, Wordmark } from '../components/Logo'
import { CheckIcon, SearchIcon, WhatsappIcon, XIcon } from '../components/icons'
import '../styles/landing.css'

const WHATSAPP = '5547991120404'
const EMAIL = 'phsvscheidt2003@gmail.com'
const LINK_WHATS = `https://wa.me/${WHATSAPP}`
const LINK_EMAIL = `mailto:${EMAIL}`

// ── conteúdo ────────────────────────────────────────────────────────────────

const ANCORAS = [
  { href: '#recursos', texto: 'Recursos' },
  { href: '#manutencao', texto: 'Manutenção' },
  { href: '#permissoes', texto: 'Permissões' },
  { href: '#implantacao', texto: 'Implantação' },
  { href: '#duvidas', texto: 'Dúvidas' },
]

const MENU_MOCK = ['Visão geral', 'Motoristas', 'Veículos', 'Rotas', 'Manutenções', 'Usuários']
const MENU_ATIVO = 'Veículos'

type Tom = 'painel' | 'neutro' | 'vencendo' | 'alerta'

const CLASSE_ETIQ: Record<Tom, string> = {
  painel: 'lp-etiq lp-etiq-painel',
  neutro: 'lp-etiq lp-etiq-neutro',
  vencendo: 'lp-etiq lp-etiq-vencendo',
  alerta: 'lp-etiq lp-etiq-alerta',
}

const VEICULOS_MOCK: { placa: string; modelo: string; km: string; motorista: string; situacao: string; tom: Tom }[] = [
  { placa: 'MHT-4G21', modelo: 'Ford Cargo 816', km: '148.230', motorista: 'Ana Ribeiro', situacao: 'Em rota', tom: 'painel' },
  { placa: 'LZP-9D14', modelo: 'VW Constellation', km: '312.940', motorista: 'Ivo Nascimento', situacao: 'Em rota', tom: 'painel' },
  { placa: 'QKA-7B08', modelo: 'VW Delivery', km: '96.510', motorista: 'Carlos Deppe', situacao: 'Disponível', tom: 'neutro' },
  { placa: 'RTB-2C55', modelo: 'Mercedes Sprinter', km: '74.180', motorista: 'Marta Lins', situacao: 'Disponível', tom: 'neutro' },
  { placa: 'MJU-5F71', modelo: 'VW Saveiro', km: '51.988', motorista: 'Helena Cruz', situacao: 'Revisão', tom: 'vencendo' },
  { placa: 'PGE-1H47', modelo: 'Fiat Fiorino', km: '38.420', motorista: 'Régis Alves', situacao: 'Disponível', tom: 'neutro' },
]

// Rótulo de campo + a falha que ele sofre na planilha. O rótulo nomeia o dado,
// não a posição — não é uma sequência, então não é numerado.
const FALHAS = [
  {
    campo: 'Quilometragem',
    texto: 'A km de cada veículo mora em três lugares — e nenhum deles está atualizado.',
  },
  {
    campo: 'Responsável',
    texto: 'Ninguém sabe de cabeça quem estava com o caminhão na última viagem.',
  },
  {
    campo: 'Permissão',
    texto: 'A planilha é compartilhada com a equipe toda, então qualquer um apaga qualquer coisa.',
  },
  {
    campo: 'Cadastro',
    texto: 'CPF errado, motorista duplicado, cadastro de gente que já saiu da empresa.',
  },
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
    inicial: 'MT',
    titulo: 'Motoristas',
    texto: 'Cadastro validado de verdade: CPF conferido dígito a dígito e idade mínima checada na hora.',
    itens: [
      'E-mail e CPF únicos na sua empresa',
      'Histórico de admissão por motorista',
      'Vínculo direto com as rotas rodadas',
    ],
  },
  {
    inicial: 'VE',
    titulo: 'Veículos',
    texto: 'Placa, marca, quilometragem e o rastro de quem rodou por último com ele.',
    itens: ['Quilometragem sempre no cadastro', 'Último motorista e última viagem', 'Frota inteira em uma lista só'],
  },
  {
    inicial: 'RO',
    titulo: 'Rotas',
    texto: 'Origem, destino, motorista e veículo — abertas ou encerradas, com datas.',
    itens: ['Rota ativa x encerrada', 'Motorista e veículo vinculados', 'Qualquer membro da equipe pode lançar'],
  },
  {
    inicial: 'MN',
    titulo: 'Manutenções',
    texto: 'Manutenção preventiva por quilometragem: você agenda, o painel avisa quando está vencendo.',
    itens: [
      'Pendentes primeiro, vencendo no topo',
      'Catálogo de tipos com intervalo em km',
      'Concluir já atualiza a km do veículo',
    ],
  },
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

// Mesma ordem das colunas do <thead> da matriz — é o que dá a cada coluna uma
// identidade estável (o nome do papel), em vez de depender da posição no array.
const PAPEIS_DA_MATRIZ = ['admin', 'supervisor', 'operador'] as const

const MATRIZ = [
  { acao: 'Ver tudo da empresa', admin: true, supervisor: true, operador: true },
  { acao: 'Lançar e editar rotas', admin: true, supervisor: true, operador: true },
  { acao: 'Cadastrar motoristas e veículos', admin: true, supervisor: true, operador: false },
  { acao: 'Agendar e concluir manutenções', admin: true, supervisor: true, operador: false },
  { acao: 'Excluir qualquer registro', admin: true, supervisor: false, operador: false },
  { acao: 'Convidar e gerenciar usuários', admin: true, supervisor: false, operador: false },
]

const GARANTIAS = [
  {
    titulo: 'Sua empresa, seus dados',
    texto: 'O acesso de cada usuário carrega a empresa dele. Dado de outra empresa simplesmente não existe para ele.',
  },
  {
    titulo: 'Saída imediata',
    texto: 'Tirar o acesso de alguém ou trocar o papel dele derruba a sessão aberta na hora.',
  },
  {
    titulo: 'Só quem foi convidado',
    texto: 'Não existe cadastro aberto. Toda conta nasce de um convite com validade e uso único.',
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

const DUVIDAS = [
  { p: 'Preciso instalar algo?', r: 'Não. O painel roda no navegador — computador do escritório, notebook ou celular.' },
  {
    p: 'Tenho só 8 veículos. É grande demais para mim?',
    r: 'O painel é o mesmo para 8 ou 300 veículos, e com frota pequena a implantação leva um dia. São justamente esses casos que a planilha bagunça primeiro.',
  },
  {
    p: 'Meu motorista não usa aplicativo. Isso é um problema?',
    r: 'Não. Quem lança rota é o escritório; o motorista só aparece como cadastro. Se um dia quiser, ele entra como operador.',
  },
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

// ── odômetro ────────────────────────────────────────────────────────────────

const KM_INICIAL = 51_780
const KM_FINAL = 51_988
const KM_PREVISTO = 52_000
/** Dentro desta faixa a manutenção já aparece como "vencendo". */
const FAIXA_AVISO = 500

function prefereMenosMovimento() {
  return window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
}

/**
 * Conta a quilometragem de KM_INICIAL até KM_FINAL. Quem pediu menos movimento
 * recebe o valor final direto — o estado da manutenção é a mensagem, não a
 * animação.
 */
function useOdometro() {
  // Quem pediu menos movimento já começa no valor final — nem chega a ver a
  // contagem, em vez de vê-la saltar depois do primeiro render.
  const [km, setKm] = useState(() => (prefereMenosMovimento() ? KM_FINAL : KM_INICIAL))
  // O id do interval mora numa ref (não numa variável local do effect) porque quem o
  // encerra é um SEGUNDO effect, que só sabe que chegou a hora depois de reagir à
  // mudança de `km` — precisa achar o mesmo interval que o primeiro effect criou.
  const intervaloRef = useRef<number | null>(null)

  useEffect(() => {
    if (prefereMenosMovimento()) return
    const inicio = window.setTimeout(() => {
      intervaloRef.current = window.setInterval(() => {
        // Atualizador puro: só calcula o próximo valor. Antes ele também decidia e
        // executava o clearInterval aqui dentro — um efeito colateral escondido num
        // updater, que o React pode reexecutar mais de uma vez para o mesmo passo.
        setKm((atual) => Math.min(atual + 4, KM_FINAL))
      }, 90)
    }, 700)
    return () => {
      window.clearTimeout(inicio)
      if (intervaloRef.current !== null) window.clearInterval(intervaloRef.current)
    }
  }, [])

  // O clearInterval mora aqui agora: reage ao `km` já commitado, fora do updater.
  useEffect(() => {
    if (km >= KM_FINAL && intervaloRef.current !== null) {
      window.clearInterval(intervaloRef.current)
      intervaloRef.current = null
    }
  }, [km])

  return km
}

/** Um dígito do odômetro: fita de 0–9 que desliza até o algarismo certo. */
function Digito({ valor }: { valor: string }) {
  return (
    <span className="lp-odo-digito">
      <span className="lp-odo-fita" style={{ '--digito': valor } as CSSProperties}>
        {['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'].map((d) => (
          <span key={d}>{d}</span>
        ))}
      </span>
    </span>
  )
}

// A casa de cada dígito (centena de milhar → unidade) é a identidade estável dele:
// o km sobe, mas a casa das centenas continua sendo a casa das centenas do início ao
// fim da contagem — é o que permite o dígito deslizar de um valor para o outro em vez
// de trocar de elemento (índice do array faria a mesma coisa aqui, mas nomear a casa
// deixa explícito por que é seguro, em vez de depender implicitamente da posição).
const CASAS_DO_ODOMETRO = ['cem-mil', 'dez-mil', 'mil', 'cem', 'dez', 'um'] as const

/**
 * Odômetro de 6 casas, sem separador de milhar — é assim que o instrumento
 * real mostra, e a largura fica fixa enquanto conta. O rótulo acessível traz o
 * número formatado em pt-BR, que é como se lê em voz alta.
 */
function Odometro({ km }: { km: number }) {
  const casas = String(km).padStart(6, '0').split('')
  return (
    <div className="lp-odo" role="img" aria-label={`Quilometragem atual: ${km.toLocaleString('pt-BR')} quilômetros`}>
      {casas.map((c, i) => (
        <Digito key={CASAS_DO_ODOMETRO[i]} valor={c} />
      ))}
      <span className="lp-odo-unidade" aria-hidden="true">
        km
      </span>
    </div>
  )
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
  forte = false,
  estreito = false,
  children,
}: {
  id?: string
  forte?: boolean
  estreito?: boolean
  children: ReactNode
}) {
  return (
    <section id={id} className={forte ? 'lp-faixa-forte' : 'lp-faixa'} style={id ? { scrollMarginTop: 72 } : undefined}>
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

// ── seções ──────────────────────────────────────────────────────────────────
// Cada `Faixa` da página é só HTML + os arrays de conteúdo lá em cima — nenhuma
// delas guarda estado próprio (a única exceção, o odômetro, chega pronta via
// prop). Extraídas para módulo por serem, juntas, o motivo de `LandingPage`
// passar de 300 linhas sem nenhuma delas ter lógica que justifique inline.

function CabecalhoLanding() {
  return (
    <header className="lp-nav">
      <div className="lp-nav-inner">
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
      </div>
    </header>
  )
}

/** Hero: o odômetro conta até a revisão avisar — é a mecânica do produto inteira, sem texto. */
function HeroLanding({ km }: { km: number }) {
  const faltam = KM_PREVISTO - km
  const estado = faltam <= 0 ? 'atrasada' : faltam <= FAIXA_AVISO ? 'vencendo' : 'ok'
  const textoEstado =
    estado === 'atrasada'
      ? `Atrasada · ${Math.abs(faltam).toLocaleString('pt-BR')} km`
      : estado === 'vencendo'
        ? `Vencendo · faltam ${faltam.toLocaleString('pt-BR')} km`
        : `Faltam ${faltam.toLocaleString('pt-BR')} km`

  return (
    // Os números são ilustrativos e vêm marcados assim.
    <section className="lp-faixa-forte">
      <div className="lp-wrap lp-hero">
        <div className="lp-hero-grade">
          <div className="lp-hero-texto">
            <span className="lp-campo lp-campo-painel">Manutenção preventiva por quilometragem</span>
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

          <div className="lp-instrumento">
            <div className="lp-instrumento-topo">
              <span className="lp-instrumento-placa">MJU-5F71</span>
              <span className="lp-instrumento-modelo">VW Saveiro</span>
              <span className="lp-instrumento-selo">Demonstração</span>
            </div>
            <div className="lp-instrumento-corpo">
              <Odometro km={km} />
              <div className="lp-servico">
                <span className="lp-servico-nome">Troca de óleo</span>
                <span className="lp-servico-prev">Prevista para {KM_PREVISTO.toLocaleString('pt-BR')} km</span>
                {/* Sem `aria-live`: o texto muda a cada tique da contagem e
                    viraria dezenas de anúncios. O estado final fica no DOM
                    como texto normal, que é o que importa ler. */}
                <span className={`lp-servico-estado${estado === 'ok' ? '' : ` is-${estado}`}`}>
                  <span className="lp-farol" aria-hidden="true" />
                  {textoEstado}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

/** Mock do painel: dados ilustrativos, não vêm da API. */
function PainelMockFaixa() {
  return (
    <Faixa>
      <span className="lp-campo">O painel</span>
      <div className="lp-mock">
        <aside className="lp-mock-aside">
          <div className="lp-brand" style={{ padding: '0 8px', marginRight: 0 }}>
            <Marca tamanho={20} />
          </div>
          <nav style={{ display: 'flex', flexDirection: 'column', gap: 2 }} aria-hidden="true">
            {MENU_MOCK.map((item) => (
              <div key={item} className={`lp-mock-item${item === MENU_ATIVO ? ' is-ativo' : ''}`}>
                <span className="lp-mock-quad" />
                {item}
              </div>
            ))}
          </nav>
          <div className="lp-mock-user">
            <span className="lp-avatar">PD</span>
            <div>
              Paulo D.
              <div style={{ color: 'var(--tinta-fraca)' }}>Admin</div>
            </div>
          </div>
        </aside>

        <div>
          <div className="lp-mock-cab">
            <span className="lp-mock-titulo">Veículos</span>
            <span className="lp-contagem">42 cadastrados</span>
            <div className="lp-direita">
              <span className="lp-mock-busca" aria-hidden="true">
                <SearchIcon size={14} />
                Buscar placa ou modelo
              </span>
              <span className="lp-mock-btn" aria-hidden="true">
                Novo veículo
              </span>
            </div>
          </div>
          <Rolagem rotulo="Exemplo da lista de veículos">
            <table className="lp-tabela">
              <thead>
                <tr>
                  <th>Placa</th>
                  <th>Modelo</th>
                  <th className="lp-numero-dir">Quilometragem</th>
                  <th>Último motorista</th>
                  <th>Situação</th>
                </tr>
              </thead>
              <tbody>
                {VEICULOS_MOCK.map((v) => (
                  <tr key={v.placa}>
                    <td className="lp-forte">{v.placa}</td>
                    <td>{v.modelo}</td>
                    <td className="lp-numero lp-numero-dir">{v.km}</td>
                    <td>{v.motorista}</td>
                    <td>
                      <span className={CLASSE_ETIQ[v.tom]}>{v.situacao}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Rolagem>
          <div className="lp-mock-rodape">Última atualização hoje, 14:20 — por Ana Ribeiro</div>
        </div>
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
        {FALHAS.map((f) => (
          <div key={f.campo}>
            <span className="lp-falha-rotulo">{f.campo}</span>
            <p className="lp-falha-texto">{f.texto}</p>
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
      </Rolagem>
    </Faixa>
  )
}

function RecursosFaixa() {
  return (
    <Faixa id="recursos">
      <span className="lp-campo lp-campo-painel">Recursos</span>
      <h2 className="lp-h2" style={{ maxWidth: '24ch' }}>
        Quatro cadastros, uma operação inteira sob controle
      </h2>
      <div className="lp-grade lp-grade-2" style={{ marginTop: 40 }}>
        {RECURSOS.map((r) => (
          <div key={r.titulo}>
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

function ManutencaoFaixa() {
  return (
    <Faixa id="manutencao">
      <span className="lp-campo lp-campo-painel">Manutenção</span>
      <h2 className="lp-h2" style={{ maxWidth: '22ch' }}>
        O que está vencendo sobe para o topo
      </h2>
      <p className="lp-lead">
        O painel compara a quilometragem prevista de cada manutenção com a km atual do veículo. Quando concluir, você
        informa a km real e o custo — e a quilometragem do veículo sobe junto, sem atualizar em dois lugares.
      </p>

      <div className="lp-mock" style={{ marginTop: 40, gridTemplateColumns: '1fr' }}>
        <div>
          <div className="lp-mock-cab">
            <span className="lp-mock-titulo">Manutenções</span>
            <span className="lp-contagem">4 em aberto</span>
            <div className="lp-direita">
              <span className="lp-mock-btn" aria-hidden="true">
                Nova manutenção
              </span>
            </div>
          </div>
          <Rolagem rotulo="Exemplo da lista de manutenções">
            <table className="lp-tabela">
              <thead>
                <tr>
                  <th>Veículo</th>
                  <th>Tipo</th>
                  <th className="lp-numero-dir">Prevista</th>
                  <th>Andamento</th>
                  <th>Situação</th>
                </tr>
              </thead>
              <tbody>
                {MANUTENCOES_MOCK.map((m) => (
                  <tr key={`${m.placa}-${m.tipo}`}>
                    <td>
                      <span className="lp-forte">{m.placa}</span>
                      <div className="lp-sub">{m.modelo}</div>
                    </td>
                    <td>{m.tipo}</td>
                    <td className="lp-numero lp-numero-dir">{m.km}</td>
                    <td className="lp-numero">{m.andamento}</td>
                    <td>
                      <span className={CLASSE_ETIQ[m.tom]}>{m.situacao}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Rolagem>
          <div className="lp-mock-rodape">
            Catálogo da empresa:{' '}
            {TIPOS_MOCK.map((t) => `${t.nome} a cada ${t.intervalo}`).join(' · ')}
          </div>
        </div>
      </div>
    </Faixa>
  )
}

function PermissoesFaixa() {
  return (
    <Faixa id="permissoes">
      <span className="lp-campo lp-campo-painel">Acesso</span>
      <div className="lp-split" style={{ marginTop: 8 }}>
        <div>
          <h2 className="lp-h2">Cada pessoa vê e faz exatamente o que deve</h2>
          <p className="lp-lead">
            Três papéis prontos, sem tela de configuração complicada. Quem entra por convite já chega com o papel
            certo.
          </p>
          <div style={{ marginTop: 32 }}>
            {GARANTIAS.map((g) => (
              <div key={g.titulo} className="lp-garantia">
                <span className="lp-garantia-titulo">{g.titulo}</span>
                <p>{g.texto}</p>
              </div>
            ))}
          </div>
        </div>
        <Rolagem rotulo="Matriz de permissões por papel">
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
                      papel (a mesma ordem do <thead> acima), não por posição: a coluna
                      "Admin" é sempre a coluna "Admin", com ou sem índice de array. */}
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
        </Rolagem>
      </div>
    </Faixa>
  )
}

function ImplantacaoFaixa() {
  return (
    <Faixa id="implantacao">
      <span className="lp-campo">Implantação</span>
      <h2 className="lp-h2" style={{ maxWidth: '22ch' }}>
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

function DuvidasFaixa() {
  return (
    <Faixa id="duvidas" estreito>
      <span className="lp-campo">Dúvidas</span>
      <h2 className="lp-h2">O que a gente mais escuta na primeira conversa</h2>
      <div style={{ marginTop: 36 }}>
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
    <section id="demonstracao" className="lp-cta" style={{ scrollMarginTop: 72 }}>
      <div className="lp-wrap">
        <div className="lp-cta-grade">
          <div>
            <span className="lp-campo">Demonstração</span>
            <h2 className="lp-h2">Mostre sua frota. A gente mostra o painel.</h2>
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
    </section>
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
  const km = useOdometro()

  return (
    <div className="lp">
      <CabecalhoLanding />
      <HeroLanding km={km} />
      <PainelMockFaixa />
      <FalhasFaixa />
      <ComparativoFaixa />
      <RecursosFaixa />
      <ManutencaoFaixa />
      <PermissoesFaixa />
      <ImplantacaoFaixa />
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
        <p className="lp-form-ok">
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
          <button type="submit" className="lp-btn lp-btn-primario" style={{ padding: 14 }}>
            Quero ver o painel
          </button>
          <p className="lp-form-nota">Sem compromisso. Usamos seus dados apenas para entrar em contato.</p>
        </div>
      )}
    </form>
  )
}
