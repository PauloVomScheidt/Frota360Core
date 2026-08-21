import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeftIcon } from '../components/icons'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [submitted, setSubmitted] = useState(false)

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    // A API ainda não possui endpoint de recuperação de senha.
    setSubmitted(true)
  }

  return (
    <div className="flex min-h-screen items-center justify-center p-10">
      <form onSubmit={handleSubmit} className="flex w-full max-w-[380px] flex-col gap-5">
        <Link
          to="/login"
          className="flex w-fit items-center gap-1.5 text-[13px] no-underline hover:!text-[var(--color-accent-700)]"
          style={{ color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}
        >
          <ArrowLeftIcon size={14} /> Voltar para login
        </Link>

        <div>
          <div
            className="mb-2 text-[11px] uppercase"
            style={{ letterSpacing: '0.1em', color: 'var(--color-accent-700)' }}
          >
            Recuperar acesso
          </div>
          <h2 style={{ margin: '0 0 8px' }}>Esqueci minha senha</h2>
          <p className="m-0 text-[13px]" style={{ color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}>
            Informe o e-mail da sua conta. Enviaremos um link para redefinir sua senha.
          </p>
        </div>

        <div className="field">
          <label htmlFor="email">E-mail corporativo</label>
          <input
            id="email"
            className="input"
            type="email"
            placeholder="voce@empresa.com"
            autoComplete="email"
            required
            style={{ borderRadius: 0 }}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>

        <button type="submit" className="btn btn-primary w-full justify-center" style={{ borderRadius: 0, padding: 12 }}>
          Enviar link de redefinição
        </button>

        {submitted && (
          <p className="m-0 text-[13px]" style={{ color: 'var(--color-accent-700)' }}>
            A redefinição automática ainda não está disponível. Fale com o administrador do sistema para
            recuperar seu acesso.
          </p>
        )}
      </form>
    </div>
  )
}
