import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { login } from '../api/auth'
import { ApiError } from '../api/http'
import { EyeIcon, EyeOffIcon } from '../components/icons'

const brandPanelStyle: React.CSSProperties = {
  backgroundColor: 'var(--color-accent-800)',
  backgroundImage:
    'repeating-linear-gradient(0deg, color-mix(in srgb, #fdfaf6 6%, transparent) 0px, transparent 1px, transparent 64px, color-mix(in srgb, #fdfaf6 6%, transparent) 65px)',
}

function errorMessages(error: unknown): string[] {
  if (error instanceof ApiError) {
    return error.erros.length > 0 ? error.erros : [error.message]
  }
  return ['Não foi possível entrar. Verifique suas credenciais e tente novamente.']
}

export function LoginPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [showPassword, setShowPassword] = useState(false)

  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: () => navigate('/', { replace: true }),
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
        <div className="flex items-center gap-2.5">
          <div className="h-[22px] w-[22px] flex-none" style={{ backgroundColor: '#fdfaf6' }} />
          <span
            className="text-base font-extrabold tracking-tight"
            style={{ fontFamily: 'var(--font-heading)', color: '#fdfaf6' }}
          >
            FROTA 360
          </span>
        </div>
        <div>
          <div
            className="mb-3.5 text-xs uppercase"
            style={{ letterSpacing: '0.14em', color: 'color-mix(in srgb, #fdfaf6 60%, transparent)' }}
          >
            Painel industrial
          </div>
          <h1
            className="max-w-[440px]"
            style={{ fontSize: 58, color: '#d8bfa0', lineHeight: 1.02, letterSpacing: '-0.02em', margin: 0 }}
          >
            Gestão de <span style={{ color: '#bfa07c' }}>frota industrial</span> em um único painel.
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
            <ul className="m-0 list-none p-0 text-[13px]" style={{ color: '#a03123' }}>
              {errorMessages(loginMutation.error).map((msg) => (
                <li key={msg}>{msg}</li>
              ))}
            </ul>
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
            Ainda não tem conta?{' '}
            <Link
              to="/criar-conta"
              className="font-semibold no-underline hover:underline"
              style={{ color: 'var(--color-accent-700)' }}
            >
              Criar conta
            </Link>
          </p>
        </form>
      </div>
    </div>
  )
}
