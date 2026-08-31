import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { esqueciSenha } from '../api/auth'
import { mensagensDeErro } from '../api/errors'
import { ErrorList } from '../components/AppLayout'
import { AuthHeading, AuthScreen } from '../components/AuthScreen'
import { ArrowLeftIcon } from '../components/icons'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')

  // A API responde 200 neutro mesmo para e-mail inexistente — a tela reflete isso.
  // Sem invalidação de propósito: só dispara um e-mail, não muda nada em cache — não
  // há sessão nem dado de empresa envolvido (rota 100% anônima).
  const enviarMutation = useMutation({ mutationFn: esqueciSenha })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    enviarMutation.mutate({ email })
  }

  return (
    <AuthScreen onSubmit={handleSubmit}>
      <Link
        to="/login"
        className="flex w-fit items-center gap-1.5 text-[13px] no-underline hover:!text-[var(--color-accent-700)]"
        style={{ color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}
      >
        <ArrowLeftIcon size={14} /> Voltar para login
      </Link>

      <AuthHeading
        kicker="Recuperar acesso"
        titulo="Esqueci minha senha"
        descricao="Informe o e-mail da sua conta. Enviaremos um link para redefinir sua senha."
      />

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

      {enviarMutation.isError && (
        <ErrorList mensagens={mensagensDeErro(enviarMutation.error, 'Não foi possível enviar o link.')} />
      )}

      <button
        type="submit"
        className="btn btn-primary w-full justify-center"
        style={{ borderRadius: 0, padding: 12 }}
        disabled={enviarMutation.isPending}
      >
        {enviarMutation.isPending ? 'Enviando…' : 'Enviar link de redefinição'}
      </button>

      {enviarMutation.isSuccess && (
        <p className="m-0 text-[13px]" style={{ color: 'var(--color-accent-700)' }}>
          {enviarMutation.data ||
            'Se o e-mail estiver cadastrado, enviamos um link de redefinição. O link vale por 30 minutos.'}
        </p>
      )}
    </AuthScreen>
  )
}
