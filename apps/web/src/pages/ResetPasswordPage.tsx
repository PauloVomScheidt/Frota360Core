import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { redefinirSenha } from '../api/auth'
import { mensagensDeErro } from '../api/errors'
import { validarSenha } from '../auth/senha'
import { ErrorList } from '../components/AppLayout'
import { AuthHeading, AuthScreen } from '../components/AuthScreen'
import { ArrowLeftIcon } from '../components/icons'

/** Rota `/redefinir-senha?token=...` — destino do link enviado por e-mail. */
export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()

  const [novaSenha, setNovaSenha] = useState('')
  const [confirmar, setConfirmar] = useState('')
  const [errosLocais, setErrosLocais] = useState<string[]>([])

  const redefinirMutation = useMutation({
    mutationFn: redefinirSenha,
    // O reset revoga as sessões antigas: o usuário precisa entrar de novo.
    onSuccess: () => setTimeout(() => navigate('/login', { replace: true }), 2500),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const erros = validarSenha(novaSenha, confirmar)
    setErrosLocais(erros)
    if (erros.length === 0) redefinirMutation.mutate({ token, novaSenha })
  }

  if (!token) {
    return (
      <AuthScreen>
        <AuthHeading
          kicker="Recuperar acesso"
          titulo="Link inválido"
          descricao="Este link de redefinição está incompleto. Solicite um novo em “Esqueci minha senha”."
        />
        <Link
          to="/esqueci-senha"
          className="btn btn-primary w-full justify-center"
          style={{ borderRadius: 0, padding: 12 }}
        >
          Solicitar novo link
        </Link>
      </AuthScreen>
    )
  }

  if (redefinirMutation.isSuccess) {
    return (
      <AuthScreen>
        <AuthHeading
          kicker="Recuperar acesso"
          titulo="Senha redefinida"
          descricao="Sua senha foi alterada e as sessões antigas foram encerradas. Redirecionando para o login…"
        />
        <Link
          to="/login"
          className="btn btn-primary w-full justify-center"
          style={{ borderRadius: 0, padding: 12 }}
        >
          Ir para o login
        </Link>
      </AuthScreen>
    )
  }

  const erros =
    errosLocais.length > 0
      ? errosLocais
      : redefinirMutation.isError
        ? mensagensDeErro(redefinirMutation.error, 'Não foi possível redefinir a senha.')
        : []

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
        titulo="Definir nova senha"
        descricao="Escolha uma senha com no mínimo 6 caracteres, incluindo 1 letra maiúscula e 1 número."
      />

      <div className="flex flex-col gap-3.5">
        <div className="field">
          <label htmlFor="novaSenha">Nova senha</label>
          <input
            id="novaSenha"
            className="input"
            type="password"
            placeholder="Crie uma senha"
            autoComplete="new-password"
            required
            style={{ borderRadius: 0 }}
            value={novaSenha}
            onChange={(e) => setNovaSenha(e.target.value)}
          />
        </div>
        <div className="field">
          <label htmlFor="confirmar">Confirmar nova senha</label>
          <input
            id="confirmar"
            className="input"
            type="password"
            placeholder="Repita a senha"
            autoComplete="new-password"
            required
            style={{ borderRadius: 0 }}
            value={confirmar}
            onChange={(e) => setConfirmar(e.target.value)}
          />
        </div>
      </div>

      <ErrorList mensagens={erros} />

      <button
        type="submit"
        className="btn btn-primary w-full justify-center"
        style={{ borderRadius: 0, padding: 12 }}
        disabled={redefinirMutation.isPending}
      >
        {redefinirMutation.isPending ? 'Salvando…' : 'Redefinir senha'}
      </button>
    </AuthScreen>
  )
}
