import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { register } from '../api/auth'
import { ApiError } from '../api/http'

function validate(nome: string, senha: string, confirmar: string, aceite: boolean): string[] {
  const erros: string[] = []
  if (nome.trim().length < 2) erros.push('Informe seu nome completo.')
  if (senha.length < 6) erros.push('A senha deve ter no mínimo 6 caracteres.')
  if (!/[A-Z]/.test(senha)) erros.push('A senha deve conter ao menos 1 letra maiúscula.')
  if (!/\d/.test(senha)) erros.push('A senha deve conter ao menos 1 número.')
  if (senha !== confirmar) erros.push('As senhas não conferem.')
  if (!aceite) erros.push('É preciso aceitar os termos de uso.')
  return erros
}

function serverErrors(error: unknown): string[] {
  if (error instanceof ApiError) {
    return error.erros.length > 0 ? error.erros : [error.message]
  }
  return ['Não foi possível criar a conta. Tente novamente.']
}

export function RegisterPage() {
  const navigate = useNavigate()
  const [nome, setNome] = useState('')
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [confirmar, setConfirmar] = useState('')
  const [aceite, setAceite] = useState(false)
  const [clientErrors, setClientErrors] = useState<string[]>([])

  const registerMutation = useMutation({
    mutationFn: register,
    onSuccess: () => navigate('/', { replace: true }),
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const erros = validate(nome, senha, confirmar, aceite)
    setClientErrors(erros)
    if (erros.length === 0) {
      registerMutation.mutate({ nome: nome.trim(), email, senha })
    }
  }

  const erros = clientErrors.length > 0
    ? clientErrors
    : registerMutation.isError
      ? serverErrors(registerMutation.error)
      : []

  return (
    <div className="flex min-h-screen items-center justify-center p-10">
      <form onSubmit={handleSubmit} className="flex w-full max-w-[400px] flex-col gap-5">
        <div>
          <div
            className="mb-2 text-[11px] uppercase"
            style={{ letterSpacing: '0.1em', color: 'var(--color-accent-700)' }}
          >
            Nova conta
          </div>
          <h2 style={{ margin: '0 0 8px' }}>Criar sua conta</h2>
          <p className="m-0 text-[13px]" style={{ color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}>
            Crie seu acesso ao painel de frota. Informe seu nome, e-mail e defina uma senha para continuar.
          </p>
        </div>

        <div className="flex flex-col gap-3.5">
          <div className="field">
            <label htmlFor="nome">Nome completo</label>
            <input
              id="nome"
              className="input"
              type="text"
              placeholder="Seu nome"
              autoComplete="name"
              required
              style={{ borderRadius: 0 }}
              value={nome}
              onChange={(e) => setNome(e.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="email">E-mail</label>
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
          <div className="field">
            <label htmlFor="senha">Senha</label>
            <input
              id="senha"
              className="input"
              type="password"
              placeholder="Crie uma senha"
              autoComplete="new-password"
              required
              style={{ borderRadius: 0 }}
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="confirmar">Confirmar senha</label>
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
          <label
            className="flex cursor-pointer items-start gap-2 text-xs"
            style={{ color: 'color-mix(in srgb, var(--color-text) 65%, transparent)' }}
          >
            <input
              type="checkbox"
              className="mt-0.5"
              style={{ accentColor: 'var(--color-accent)' }}
              checked={aceite}
              onChange={(e) => setAceite(e.target.checked)}
            />
            Li e aceito os termos de uso e a política de privacidade.
          </label>
        </div>

        {erros.length > 0 && (
          <ul className="m-0 list-none p-0 text-[13px]" style={{ color: '#a03123' }}>
            {erros.map((msg) => (
              <li key={msg}>{msg}</li>
            ))}
          </ul>
        )}

        <button
          type="submit"
          className="btn btn-primary w-full justify-center"
          style={{ borderRadius: 0, padding: 12 }}
          disabled={registerMutation.isPending}
        >
          {registerMutation.isPending ? 'Criando conta…' : 'Criar conta'}
        </button>

        <p
          className="m-0 text-center text-[13px]"
          style={{ color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}
        >
          Já tem conta?{' '}
          <Link
            to="/login"
            className="font-semibold no-underline hover:underline"
            style={{ color: 'var(--color-accent-700)' }}
          >
            Entrar
          </Link>
        </p>
      </form>
    </div>
  )
}
