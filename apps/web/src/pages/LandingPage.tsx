import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { LogoMark, Wordmark } from '../components/Logo'
import { InstagramIcon, MailIcon, RouteIcon, TruckIcon, WhatsappIcon } from '../components/icons'

const divider = '2px solid var(--color-divider)'
const kicker = 'var(--color-accent-700)'

const painelListrado = {
  backgroundColor: 'var(--color-accent-800)',
  backgroundImage:
    'repeating-linear-gradient(0deg, color-mix(in srgb, #fdfaf6 6%, transparent) 0px, transparent 1px, transparent 64px, color-mix(in srgb, #fdfaf6 6%, transparent) 65px)',
}

interface Feature {
  titulo: string
  texto: string
  icone: ReactNode
}

const FEATURES: Feature[] = [
  {
    titulo: 'Veículos e motoristas',
    texto:
      'Cadastro completo da frota com quilometragem, placa e histórico de quem dirigiu cada veículo.',
    icone: <TruckIcon size={24} />,
  },
  {
    titulo: 'Rotas em andamento',
    texto:
      'Origem, destino, motorista e veículo de cada rota — com início, fim e situação sempre visíveis.',
    icone: <RouteIcon size={24} />,
  },
  {
    titulo: 'Equipe por convite',
    texto:
      'Você convida por e-mail e define a permissão; o acesso nasce já na empresa e no papel certos.',
    icone: <MailIcon size={24} />,
  },
]

function BotaoEntrar({ padding }: { padding: string }) {
  return (
    <Link to="/login" className="btn btn-primary" style={{ borderRadius: 0, padding }}>
      Acesse sua conta
    </Link>
  )
}

export function LandingPage() {
  return (
    <div>
      <header
        className="flex flex-wrap items-center justify-between gap-6 px-12 py-5"
        style={{ borderBottom: divider }}
      >
        <div className="flex items-center gap-3">
          <LogoMark size={30} />
          <Wordmark size={20} />
        </div>
        <BotaoEntrar padding="11px 20px" />
      </header>

      <section className="grid lg:grid-cols-[1.15fr_1fr]" style={{ borderBottom: divider }}>
        <div className="flex flex-col items-start gap-7 px-12 py-[88px]">
          <span className="text-xs uppercase" style={{ letterSpacing: '0.14em', color: kicker }}>
            Painel industrial
          </span>
          <h1
            className="max-w-[640px]"
            style={{ fontSize: 64, lineHeight: 1.02, letterSpacing: '-0.03em', margin: 0 }}
          >
            Gestão de frota industrial em um único painel.
          </h1>
          <p
            className="m-0 max-w-[520px] text-base"
            style={{ lineHeight: 1.6, color: 'color-mix(in srgb, var(--color-text) 70%, transparent)' }}
          >
            Veículos, motoristas e rotas da sua operação em um só lugar — com permissões por equipe,
            convites por e-mail e dados isolados por empresa.
          </p>
          <div className="flex flex-wrap gap-3">
            <BotaoEntrar padding="13px 24px" />
            <a href="#contato" className="btn btn-secondary" style={{ borderRadius: 0, padding: '13px 24px' }}>
              Falar com vendas
            </a>
          </div>
        </div>

        <div className="flex flex-col justify-end gap-5 px-12 py-14" style={painelListrado}>
          <LogoMark size={56} tom="light" />
          <p
            className="m-0 max-w-[340px]"
            style={{
              fontFamily: 'var(--font-heading)',
              fontWeight: 800,
              fontSize: 26,
              lineHeight: 1.15,
              letterSpacing: '-0.02em',
              color: '#d8bfa0',
            }}
          >
            Uma operação, uma verdade: o mesmo dado para toda a equipe.
          </p>
        </div>
      </section>

      <section className="grid md:grid-cols-3" style={{ borderBottom: divider }}>
        {FEATURES.map((f) => (
          <div
            key={f.titulo}
            className="flex flex-col gap-3.5 px-10 py-11"
            style={{ borderRight: '1px solid var(--color-divider)' }}
          >
            <span style={{ color: 'var(--color-accent)' }}>{f.icone}</span>
            <h3 style={{ margin: 0, fontSize: 21 }}>{f.titulo}</h3>
            <p
              className="m-0 text-sm"
              style={{ lineHeight: 1.6, color: 'color-mix(in srgb, var(--color-text) 65%, transparent)' }}
            >
              {f.texto}
            </p>
          </div>
        ))}
      </section>

      <section
        className="flex flex-wrap items-end justify-between gap-6 px-12 py-16"
        style={{ borderBottom: divider }}
      >
        <div>
          <span className="text-xs uppercase" style={{ letterSpacing: '0.14em', color: kicker }}>
            Controle de acesso
          </span>
          <h2 className="max-w-[620px]" style={{ margin: '14px 0 0' }}>
            Admin, Supervisor e Operador — cada um vê e faz exatamente o que deve.
          </h2>
        </div>
        <BotaoEntrar padding="13px 24px" />
      </section>

      <footer id="contato">
        <div className="flex flex-wrap justify-between gap-8 px-12 pt-10 pb-7">
          <div className="flex flex-col gap-2.5">
            <FooterTitulo>Frota 360</FooterTitulo>
            <FooterLink>Quem somos</FooterLink>
          </div>
          <div className="flex flex-col gap-2.5">
            <FooterTitulo>Legal</FooterTitulo>
            <FooterLink>Política de privacidade</FooterLink>
          </div>
          <div className="flex flex-col gap-2.5">
            <FooterTitulo>Siga-nos</FooterTitulo>
            <div className="flex gap-3.5">
              <a
                href="#contato"
                aria-label="WhatsApp"
                className="flex hover:!text-[var(--color-accent-700)]"
                style={{ color: 'color-mix(in srgb, var(--color-text) 55%, transparent)' }}
              >
                <WhatsappIcon size={17} />
              </a>
              <a
                href="#contato"
                aria-label="Instagram"
                className="flex hover:!text-[var(--color-accent-700)]"
                style={{ color: 'color-mix(in srgb, var(--color-text) 55%, transparent)' }}
              >
                <InstagramIcon size={17} />
              </a>
            </div>
          </div>
        </div>
        <div
          className="flex flex-wrap items-center justify-between gap-2 px-12 py-3.5"
          style={{ borderTop: '1px solid var(--color-divider)' }}
        >
          <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-text) 50%, transparent)' }}>
            © 2026 Frota 360
          </span>
          <span className="text-xs" style={{ color: 'color-mix(in srgb, var(--color-text) 50%, transparent)' }}>
            v1.0.0
          </span>
        </div>
      </footer>
    </div>
  )
}

function FooterTitulo({ children }: { children: ReactNode }) {
  return (
    <span
      className="text-xs uppercase"
      style={{
        fontFamily: 'var(--font-heading)',
        fontWeight: 800,
        letterSpacing: '0.06em',
        color: 'color-mix(in srgb, var(--color-text) 55%, transparent)',
      }}
    >
      {children}
    </span>
  )
}

function FooterLink({ children }: { children: ReactNode }) {
  return (
    <a
      href="#contato"
      className="w-fit text-[13px] no-underline hover:!text-[var(--color-accent-700)]"
      style={{ color: 'var(--color-text)' }}
    >
      {children}
    </a>
  )
}
