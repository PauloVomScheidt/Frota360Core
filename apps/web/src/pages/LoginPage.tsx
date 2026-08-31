import { useState, type CSSProperties, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { login } from '../api/auth'
import { mensagensDeErro } from '../api/errors'
import { rotaInicial } from '../auth/permissions'
import { notificarMudancaDeSessao } from '../auth/useSession'
import { ErrorList } from '../components/AppLayout'
import { EyeIcon, EyeOffIcon } from '../components/icons'
import { LogoMark, Wordmark } from '../components/Logo'

const brandPanelStyle: CSSProperties = {
  backgroundColor: 'var(--color-accent-800)',
  backgroundImage:
    'repeating-linear-gradient(0deg, color-mix(in srgb, #fdfaf6 6%, transparent) 0px, transparent 1px, transparent 64px, color-mix(in srgb, #fdfaf6 6%, transparent) 65px)',
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const origem = (location.state as { from?: string } | null)?.from

  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [showPassword, setShowPassword] = useState(false)

  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: (auth) => {
      // `/login` não é bloqueada para quem já está autenticado — nada impede alguém de
      // trocar de conta nesta mesma aba sem recarregar a página. Sem isto, o cache do
      // React Query (chaves como `['veiculos']` não carregam a empresa) poderia mostrar,
      // por até o staleTime, dado de uma empresa que não é mais a do usuário logado.
      queryClient.clear()
      notificarMudancaDeSessao()
      // Sem uma tela de origem, o destino depende do papel: o motorista vai para as
      // rotas dele, e não para um painel de gestão que o guard devolveria.
      navigate(origem ?? rotaInicial(auth.role), { replace: true })
    },
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    loginMutation.mutate({ email, senha })
  }

  return (
    <div className="flex min-h-screen">
      <div
        className="relative hidden min-w-[320px] flex-col justify-between p-14 md:flex md:w-[44%]"
        style={{ ...brandPanelStyle, color: '#fdfaf6' }}
      >
        <Link to="/" className="flex flex-col items-start gap-2.5 no-underline" aria-label="Frota 360 — início">
          <LogoMark size={56} tom="light" />
          <Wordmark size={22} cor="#fdfaf6" corDestaque="#5c7896" />
        </Link>
        <div>
          <div
            className="mb-3.5 text-xs uppercase"
            style={{ letterSpacing: '0.14em', color: 'color-mix(in srgb, #fdfaf6 60%, transparent)' }}
          >
            Painel industrial
          </div>
          <h1
            className="max-w-[440px]"
            style={{ fontSize: 58, color: '#fdfaf6', lineHeight: 1.02, letterSpacing: '-0.02em', margin: 0 }}
          >
            Gestão de frota industrial em um único painel.
          </h1>
        </div>
        <span className="text-xs" style={{ color: 'color-mix(in srgb, #fdfaf6 55%, transparent)' }}>
          © 2026 Frota 360
        </span>
      </div>

      <div className="flex flex-1 items-center justify-center p-10">
        <form onSubmit={handleSubmit} className="flex w-full max-w-[340px] flex-col gap-7">
          <h2 style={{ margin: 0 }}>Entrar</h2>

          <div className="flex flex-col gap-4">
            <div className="field">
              <label htmlFor="email">E-mail</label>
              <input
                id="email"
                className="input input-underline"
                type="email"
                placeholder="voce@empresa.com"
                autoComplete="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </div>

            <div className="field">
              <label htmlFor="senha">Senha</label>
              <div className="relative flex">
                <input
                  id="senha"
                  className="input input-underline"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  autoComplete="current-password"
                  required
                  value={senha}
                  onChange={(e) => setSenha(e.target.value)}
                />
                <button
                  type="button"
                  className="absolute top-0 right-0 bottom-0 flex w-7 cursor-pointer items-center justify-center border-0 bg-transparent"
                  style={{ color: 'color-mix(in srgb, var(--color-text) 55%, transparent)' }}
                  onClick={() => setShowPassword((v) => !v)}
                  aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                >
                  {showPassword ? <EyeOffIcon size={16} /> : <EyeIcon size={16} />}
                </button>
              </div>
            </div>

            <Link
              to="/esqueci-senha"
              className="w-fit text-[13px] no-underline hover:!text-[var(--color-accent-700)]"
              style={{ color: 'color-mix(in srgb, var(--color-text) 55%, transparent)' }}
            >
              Esqueci minha senha
            </Link>
          </div>

          {loginMutation.isError && (
            <ErrorList mensagens={mensagensDeErro(loginMutation.error, 'Não foi possível entrar.')} />
          )}

          <button
            type="submit"
            className="btn btn-primary w-full justify-center"
            style={{ borderRadius: 0, padding: 13 }}
            disabled={loginMutation.isPending}
          >
            {loginMutation.isPending ? 'Entrando…' : 'Entrar'}
          </button>

          <p className="m-0 text-[13px]" style={{ color: 'color-mix(in srgb, var(--color-text) 55%, transparent)' }}>
            O acesso ao Frota 360 é criado por convite. Fale com o administrador da sua empresa para
            receber o seu.
          </p>
        </form>
      </div>
    </div>
  )
}
