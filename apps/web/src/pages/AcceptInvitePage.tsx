import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { aceitarConvite } from '../api/convites'
import { mensagensDeErro } from '../api/errors'
import { validarSenha } from '../auth/senha'
import { notificarMudancaDeSessao } from '../auth/useSession'
import { ErrorList } from '../components/AppLayout'
import { AuthHeading, AuthScreen } from '../components/AuthScreen'

/**
 * Rota `/convite?token=...` — destino do link enviado pelo admin.
 * O aceite já devolve a sessão autenticada, então cai direto no painel.
 */
export function AcceptInvitePage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const navigate = useNavigate()

  const [nome, setNome] = useState('')
  const [senha, setSenha] = useState('')
  const [confirmar, setConfirmar] = useState('')
  const [aceite, setAceite] = useState(false)
  const [errosLocais, setErrosLocais] = useState<string[]>([])

  const aceitarMutation = useMutation({
    mutationFn: aceitarConvite,
    onSuccess: () => {
      notificarMudancaDeSessao()
      navigate('/', { replace: true })
    },
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const erros: string[] = []
    if (nome.trim().length < 2) erros.push('Informe seu nome completo.')
    erros.push(...validarSenha(senha, confirmar))
    if (!aceite) erros.push('É preciso aceitar os termos de uso.')

    setErrosLocais(erros)
    if (erros.length === 0) {
      aceitarMutation.mutate({ token, nome: nome.trim(), senha })
    }
  }

  if (!token) {
    return (
      <AuthScreen>
        <AuthHeading
          kicker="Convite de equipe"
          titulo="Convite inválido"
          descricao="Este link de convite está incompleto. Peça ao administrador da sua empresa para reenviá-lo."
        />
        <Link to="/login" className="btn btn-secondary w-full justify-center" style={{ borderRadius: 0, padding: 12 }}>
          Ir para o login
        </Link>
      </AuthScreen>
    )
  }

  const erros =
    errosLocais.length > 0
      ? errosLocais
      : aceitarMutation.isError
        ? mensagensDeErro(aceitarMutation.error, 'Não foi possível aceitar o convite.')
        : []

  return (
    <AuthScreen largura={400} onSubmit={handleSubmit}>
      <AuthHeading
        kicker="Convite de equipe"
        titulo="Criar sua conta"
        descricao="Você foi convidado para acessar o painel de frota. Defina seu nome e uma senha para continuar — sua empresa e permissão já vêm do convite."
      />

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

      <ErrorList mensagens={erros} />

      <button
        type="submit"
        className="btn btn-primary w-full justify-center"
        style={{ borderRadius: 0, padding: 12 }}
        disabled={aceitarMutation.isPending}
      >
        {aceitarMutation.isPending ? 'Criando conta…' : 'Criar conta'}
      </button>

      <p
        className="m-0 text-center text-[13px]"
        style={{ color: 'color-mix(in srgb, var(--color-text) 60%, transparent)' }}
      >
        Já tem conta?{' '}
        <Link to="/login" className="font-semibold no-underline hover:underline" style={{ color: 'var(--color-accent-700)' }}>
          Entrar
        </Link>
      </p>
    </AuthScreen>
  )
}
