import { screen, waitFor } from '@testing-library/react'
import type { UserEvent } from '@testing-library/user-event'
import { Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { tokenStorage } from '../api/tokenStorage'
import type { ConviteConsultadoResponse, Role } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { porTextoCompleto } from '../test/consultas'
import { fabricarSessao } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { RotaAtual } from '../test/RotaAtual'
import { AcceptInvitePage } from './AcceptInvitePage'

const CONVITE: ConviteConsultadoResponse = {
  nomeEmpresa: 'Transportadora Horizonte',
  email: 'nova@horizonte.teste',
  role: 'Motorista',
  expiraEm: '2026-11-19T00:00:00',
}

function telaDeConvite(rota = '/convite?token=conv-abc') {
  return renderizar(
    <Routes>
      <Route path="/convite" element={<AcceptInvitePage />} />
      <Route path="*" element={<RotaAtual />} />
    </Routes>,
    { rota },
  )
}

interface Preenchimento {
  nome?: string
  cpf?: string
  nascimento?: string
  senha?: string
  confirmacao?: string
  aceitarTermos?: boolean
}

async function preencher(
  user: UserEvent,
  { nome = 'Nova Pessoa', cpf, nascimento, senha = 'Senha123', confirmacao = senha, aceitarTermos = true }: Preenchimento = {},
) {
  await user.type(screen.getByLabelText('Nome completo'), nome)
  if (cpf) await user.type(screen.getByLabelText('CPF (opcional)'), cpf)
  if (nascimento) await user.type(screen.getByLabelText('Data de nascimento (opcional)'), nascimento)
  await user.type(screen.getByLabelText('Senha'), senha)
  await user.type(screen.getByLabelText('Confirmar senha'), confirmacao)
  if (aceitarTermos) await user.click(screen.getByRole('checkbox', { name: /Li e aceito os termos/ }))
  await user.click(screen.getByRole('button', { name: 'Criar conta' }))
}

describe('AcceptInvitePage — consulta do convite', () => {
  it('mostra a empresa, a permissão e o e-mail vindos de POST /convite/consultar', async () => {
    const consulta = capturar('post', '/convite/consultar', responderJson(CONVITE))
    telaDeConvite()

    expect(
      await screen.findByText(
        porTextoCompleto(
          'Você foi convidado para acessar o painel de frota de Transportadora Horizonte como Motorista. Defina seu nome e uma senha para continuar.',
        ),
      ),
    ).toBeInTheDocument()
    expect(screen.getByText(porTextoCompleto('Sua conta será criada com o e-mail nova@horizonte.teste.'))).toBeInTheDocument()
    expect(consulta[0].corpo).toEqual({ token: 'conv-abc' })
    expect(consulta[0].url.search).toBe('')
  })

  it('sem token na URL, diz que o convite é inválido e nem consulta a API', () => {
    const consulta = capturar('post', '/convite/consultar', responderJson(CONVITE))
    telaDeConvite('/convite')
    expect(screen.getByRole('heading', { name: 'Convite inválido' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Ir para o login' })).toHaveAttribute('href', '/login')
    expect(consulta).toHaveLength(0)
  })

  it('token inválido, usado ou expirado (404) troca a tela inteira antes de preencher', async () => {
    capturar('post', '/convite/consultar', responderErro(404, ['Convite não encontrado.']))
    telaDeConvite()
    expect(await screen.findByRole('heading', { name: 'Convite indisponível' })).toBeInTheDocument()
    expect(screen.queryByLabelText('Nome completo')).not.toBeInTheDocument()
  })

  it('outra falha na consulta (429) não bloqueia: o formulário segue com o texto genérico', async () => {
    const consulta = capturar('post', '/convite/consultar', responderErro(429, ['Muitas tentativas.']))
    telaDeConvite()
    await waitFor(() => expect(consulta).toHaveLength(1))
    expect(screen.getByText(/sua empresa e permissão já vêm do convite/)).toBeInTheDocument()
    expect(screen.getByLabelText('Nome completo')).toBeInTheDocument()
  })
})

describe('AcceptInvitePage — validação no cliente', () => {
  it.each<[string, Preenchimento, string[]]>([
    ['nome curto', { nome: 'A' }, ['Informe seu nome completo.']],
    ['senha sem número', { senha: 'SenhaForte' }, ['A senha deve conter ao menos 1 número.']],
    ['senha curta e sem maiúscula', { senha: 'ab1' }, ['A senha deve ter no mínimo 6 caracteres.', 'A senha deve conter ao menos 1 letra maiúscula.']],
    ['confirmação diferente', { confirmacao: 'Senha124' }, ['As senhas não conferem.']],
    ['termos não aceitos', { aceitarTermos: false }, ['É preciso aceitar os termos de uso.']],
  ])('%s: recusa sem chamar a API', async (_, preenchimento, mensagens) => {
    capturar('post', '/convite/consultar', responderJson(CONVITE))
    const aceite = capturar('post', '/convite/aceitar', responderJson(fabricarSessao()))
    const { user } = telaDeConvite()

    await preencher(user, preenchimento)

    for (const mensagem of mensagens) expect(screen.getByText(mensagem)).toBeInTheDocument()
    expect(aceite).toHaveLength(0)
  })
})

describe('AcceptInvitePage — aceite', () => {
  it.each<[Role, string]>([
    ['Motorista', '/minhas-rotas'],
    ['Operador', '/dashboard'],
  ])('%s cria a conta, fica logado e cai em %s', async (role, destino) => {
    capturar('post', '/convite/consultar', responderJson({ ...CONVITE, role }))
    const sessao = fabricarSessao({ nome: 'Nova Pessoa', role, nomeEmpresa: 'Transportadora Horizonte' })
    const aceite = capturar('post', '/convite/aceitar', responderJson(sessao))
    const { user } = telaDeConvite()

    await preencher(user, { nome: '  Nova Pessoa  ' })

    expect(await screen.findByRole('status', { name: 'Rota atual' })).toHaveTextContent(destino)
    expect(aceite[0].corpo).toEqual({ token: 'conv-abc', nome: 'Nova Pessoa', senha: 'Senha123' })
    expect(tokenStorage.getUser()).toMatchObject({ role, nomeEmpresa: 'Transportadora Horizonte' })
  })

  it('CPF vai só com os dígitos, e a data de nascimento quando informada', async () => {
    capturar('post', '/convite/consultar', responderJson(CONVITE))
    const aceite = capturar('post', '/convite/aceitar', responderJson(fabricarSessao({ role: 'Motorista' })))
    const { user } = telaDeConvite()

    await user.type(screen.getByLabelText('CPF (opcional)'), '12345678901')
    expect(screen.getByLabelText('CPF (opcional)')).toHaveValue('123.456.789-01')
    await preencher(user, { nascimento: '1990-05-20' })

    await waitFor(() => expect(aceite).toHaveLength(1))
    expect(aceite[0].corpo).toMatchObject({ cpf: '12345678901', dataNascimento: '1990-05-20' })
  })

  it('o aceite limpa o cache de uma sessão anterior na mesma aba', async () => {
    capturar('post', '/convite/consultar', responderJson(CONVITE))
    capturar('post', '/convite/aceitar', responderJson(fabricarSessao({ role: 'Motorista' })))
    const { user, queryClient } = telaDeConvite()
    queryClient.setQueryData(['veiculos'], ['da outra empresa'])

    await preencher(user)

    await screen.findByRole('status', { name: 'Rota atual' })
    expect(queryClient.getQueryData(['veiculos'])).toBeUndefined()
  })

  it('a recusa da API no aceite aparece literal', async () => {
    capturar('post', '/convite/consultar', responderJson(CONVITE))
    capturar('post', '/convite/aceitar', responderErro(422, ['Este convite já foi utilizado.']))
    const { user } = telaDeConvite()

    await preencher(user)

    expect(await screen.findByText('Este convite já foi utilizado.')).toBeInTheDocument()
    expect(tokenStorage.getUser()).toBeNull()
  })
})
