import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { usuariosApi } from '../api/usuarios'
import { mensagensDeErro } from '../api/errors'
import { tokenStorage } from '../api/tokenStorage'
import { notificarMudancaDeSessao, useSession } from '../auth/useSession'
import { AppLayout, ErrorList, PageHeader } from '../components/AppLayout'
import { mascaraCpf, paraInputDate, somenteDigitos } from '../lib/format'

const mutedText = 'color-mix(in srgb, var(--color-text) 55%, transparent)'

/**
 * Rota `/perfil` — a única tela sem gate de papel: qualquer autenticado edita o próprio
 * cadastro, o Motorista inclusive, que é justamente quem tem CPF.
 *
 * É o caminho do direito de correção da LGPD (Art. 18, III). Corrigir dado pessoal alheio
 * não passa por aqui nem por lugar nenhum: o titular é quem edita.
 */
export function PerfilPage() {
  const queryClient = useQueryClient()
  const sessao = useSession()

  const perfilQuery = useQuery({ queryKey: ['perfil'], queryFn: usuariosApi.getPerfil })

  const [nome, setNome] = useState('')
  const [cpf, setCpf] = useState('')
  const [dataNascimento, setDataNascimento] = useState('')
  const [errosLocais, setErrosLocais] = useState<string[]>([])
  const [salvo, setSalvo] = useState(false)
  const [carregadoDoId, setCarregadoDoId] = useState<number | null>(null)

  // Preenche o formulário quando o perfil chega — sem isto, salvar apagaria o que já existe.
  // O ajuste é durante o render (e não num efeito) porque a fonte é o próprio estado do
  // componente, não um sistema externo; o `id` guardado impede que um refetch depois de
  // uma edição descarte o que a pessoa está digitando.
  const perfil = perfilQuery.data
  if (perfil && perfil.id !== carregadoDoId) {
    setCarregadoDoId(perfil.id)
    setNome(perfil.nome)
    setCpf(perfil.cpf ? mascaraCpf(perfil.cpf) : '')
    setDataNascimento(paraInputDate(perfil.dataNascimento))
  }

  const salvarMutation = useMutation({
    mutationFn: usuariosApi.atualizarPerfil,
    onSuccess: (atualizado) => {
      setSalvo(true)
      // As duas listas exibem nome e CPF; `perfil` é a fonte desta própria tela.
      queryClient.invalidateQueries({ queryKey: ['perfil'] })
      queryClient.invalidateQueries({ queryKey: ['motoristas'] })
      queryClient.invalidateQueries({ queryKey: ['usuarios'] })
      // O claim `name` do token só muda no próximo refresh: corrigimos a sessão local
      // para o header não continuar mostrando o nome antigo.
      tokenStorage.atualizarNome(atualizado.nome)
      notificarMudancaDeSessao()
    },
  })

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSalvo(false)

    const erros: string[] = []
    if (nome.trim().length < 2) erros.push('Informe seu nome completo.')

    const digitos = somenteDigitos(cpf)
    if (digitos.length > 0 && digitos.length !== 11) erros.push('O CPF deve ter 11 dígitos.')

    setErrosLocais(erros)
    if (erros.length > 0) return

    // Em branco vira `undefined`: o back grava nulo, e não string vazia.
    salvarMutation.mutate({
      nome: nome.trim(),
      cpf: digitos || undefined,
      dataNascimento: dataNascimento || undefined,
    })
  }

  const erros =
    errosLocais.length > 0
      ? errosLocais
      : salvarMutation.isError
        ? mensagensDeErro(salvarMutation.error, 'Não foi possível salvar o perfil.')
        : perfilQuery.isError
          ? mensagensDeErro(perfilQuery.error, 'Não foi possível carregar seu perfil.')
          : []

  return (
    <AppLayout>
      <PageHeader
        titulo="Meu perfil"
        subtitulo="Corrija seus dados pessoais. Estas informações são suas e só você as edita."
      />

      <form
        onSubmit={handleSubmit}
        className="flex max-w-[520px] flex-col gap-4 p-5"
        style={{ border: '1px solid var(--color-divider)', background: 'var(--color-surface)' }}
      >
        <div className="field">
          <label htmlFor="nome">Nome completo</label>
          <input
            id="nome"
            className="input"
            type="text"
            autoComplete="name"
            required
            disabled={perfilQuery.isPending}
            value={nome}
            onChange={(e) => setNome(e.target.value)}
          />
        </div>

        <div className="field">
          <label htmlFor="cpf">CPF (opcional)</label>
          <input
            id="cpf"
            className="input"
            type="text"
            inputMode="numeric"
            placeholder="000.000.000-00"
            autoComplete="off"
            disabled={perfilQuery.isPending}
            value={cpf}
            onChange={(e) => setCpf(mascaraCpf(e.target.value))}
          />
          <span className="text-[12px]" style={{ color: mutedText }}>
            Deixe em branco para remover o CPF do seu cadastro.
          </span>
        </div>

        <div className="field">
          <label htmlFor="dataNascimento">Data de nascimento (opcional)</label>
          <input
            id="dataNascimento"
            className="input"
            type="date"
            autoComplete="bday"
            disabled={perfilQuery.isPending}
            value={dataNascimento}
            onChange={(e) => setDataNascimento(e.target.value)}
          />
        </div>

        {/* E-mail é a chave de login e o papel é concedido pelo administrador: nenhum dos
            dois se altera aqui, mas mostrá-los evita a pergunta de onde alterar. */}
        <div className="flex flex-col gap-1 pt-1 text-[13px]" style={{ color: mutedText }}>
          <span>
            E-mail: <strong style={{ color: 'var(--color-text)' }}>{perfil?.email ?? sessao?.email ?? '—'}</strong> — é a
            chave de acesso e só muda por solicitação ao administrador.
          </span>
          <span>
            Permissão: <strong style={{ color: 'var(--color-text)' }}>{perfil?.role ?? sessao?.role ?? '—'}</strong> —
            concedida pelo administrador da empresa.
          </span>
        </div>

        <ErrorList mensagens={erros} />

        {salvo && !salvarMutation.isError && (
          <span className="text-[13px]" style={{ color: 'var(--color-accent-700)' }}>
            Perfil atualizado com sucesso.
          </span>
        )}

        <div>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={salvarMutation.isPending || perfilQuery.isPending}
          >
            {salvarMutation.isPending ? 'Salvando…' : 'Salvar alterações'}
          </button>
        </div>
      </form>
    </AppLayout>
  )
}
