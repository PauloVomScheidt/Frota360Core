import { screen, waitFor, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { LogAuditoriaResponse } from '../api/types'
import { capturar, responderErro, responderJson } from '../test/capturar'
import { fabricarLogAuditoria, fabricarUsuario, paginado } from '../test/fabricas'
import { renderizar } from '../test/renderizar'
import { AuditoriaPage } from './AuditoriaPage'

const COM_DIFF = fabricarLogAuditoria({
  id: 1,
  acao: 'Atualizou',
  entidade: 'Veiculo',
  entidadeId: 7,
  descricao: 'Atualizou o veículo ABC1D01.',
  alteracoes: [
    { campo: 'Placa', de: 'ABC1234', para: 'ABC1D01' },
    { campo: 'DataUltimaViagem', de: null, para: '2026-11-10T14:00:00' },
  ],
  ipOrigem: '10.0.0.8',
})
const EXCLUSAO = fabricarLogAuditoria({
  id: 2,
  acao: 'Excluiu',
  entidade: 'Despesa',
  entidadeId: 30,
  usuarioNome: 'Bruno Lima',
  usuarioRole: 'Admin',
  descricao: 'Excluiu a despesa de R$ 50,00.',
})

function montar(logs: LogAuditoriaResponse[] = [COM_DIFF, EXCLUSAO], total = logs.length) {
  const consultas = capturar('get', '/auditoria', ({ params }) =>
    responderJson(paginado(logs, Number(params.pagina), Number(params.tamanhoPagina), total))(),
  )
  capturar('get', '/usuario', responderJson([fabricarUsuario({ id: 5, nome: 'Bruno Lima', email: 'bruno@frota360.teste' })]))
  return { ...renderizar(<AuditoriaPage />, { rota: '/auditoria', papel: 'Admin' }), consultas }
}

const linha = (texto: string) => screen.findByRole('row', { name: new RegExp(texto) })

describe('AuditoriaPage — leitura', () => {
  it('mostra quem, o papel do momento, a ação, o registro e a descrição pronta do servidor', async () => {
    montar()
    expect(screen.getByRole('heading', { name: 'Auditoria' })).toBeInTheDocument()
    const celulas = within(await linha('Excluiu a despesa')).getAllByRole('cell')
    expect(celulas[1]).toHaveTextContent('12/11/2026, 09:30:00')
    expect(celulas[2]).toHaveTextContent('Bruno LimaAdmin')
    expect(celulas[3]).toHaveTextContent('Excluiu')
    expect(celulas[4]).toHaveTextContent('Despesa #30')
    expect(celulas[5]).toHaveTextContent('Excluiu a despesa de R$ 50,00.')
  })

  it('é somente leitura: nenhuma ação de alterar ou apagar', async () => {
    montar()
    await linha('Excluiu a despesa')
    expect(screen.queryByRole('button', { name: /Novo|Editar|Excluir/ })).not.toBeInTheDocument()
  })

  it('linha com diff abre o campo a campo, com datas em pt-BR e o IP de origem', async () => {
    const { user } = montar()
    const linhaComDiff = await screen.findByRole('button', { name: /Atualizou o veículo ABC1D01/ })
    expect(linhaComDiff).toHaveAttribute('aria-expanded', 'false')

    await user.click(linhaComDiff)

    expect(linhaComDiff).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByText('Placa').parentElement).toHaveTextContent('Placa ABC1234 → ABC1D01')
    expect(screen.getByText('DataUltimaViagem').parentElement).toHaveTextContent('DataUltimaViagem — → 10/11/2026, 14:00:00')
    expect(screen.getByText('Origem: 10.0.0.8')).toBeInTheDocument()

    await user.click(linhaComDiff)
    expect(screen.queryByText('Origem: 10.0.0.8')).not.toBeInTheDocument()
  })

  it('o diff também abre pelo teclado', async () => {
    const { user } = montar()
    const linhaComDiff = await screen.findByRole('button', { name: /Atualizou o veículo ABC1D01/ })
    linhaComDiff.focus()
    await user.keyboard('{Enter}')
    expect(screen.getByText('Origem: 10.0.0.8')).toBeInTheDocument()
    await user.keyboard(' ')
    expect(screen.queryByText('Origem: 10.0.0.8')).not.toBeInTheDocument()
  })

  it('linha sem diff não é acionável', async () => {
    montar()
    expect(await linha('Excluiu a despesa')).not.toHaveAttribute('role', 'button')
  })

  it('trilha vazia', async () => {
    montar([])
    expect(await screen.findByText(/Nada registrado ainda/)).toBeInTheDocument()
  })

  it('falha ao carregar', async () => {
    capturar('get', '/usuario', responderJson([]))
    capturar('get', '/auditoria', responderErro(500, ['Erro interno.']))
    renderizar(<AuditoriaPage />, { papel: 'Admin' })
    expect(await screen.findByText(/Erro interno\.|Não foi possível carregar a auditoria\./)).toBeInTheDocument()
  })
})

describe('AuditoriaPage — filtros e páginas', () => {
  it('filtrar por entidade, ação e pessoa vai para o servidor e volta para a página 1', async () => {
    const { user, consultas } = montar([COM_DIFF, EXCLUSAO], 40)
    await screen.findByText('1–15 de 40')
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(consultas.at(-1)?.params.pagina).toBe('2'))

    await user.selectOptions(screen.getByLabelText('O que'), 'Despesa')
    await user.selectOptions(screen.getByLabelText('Ação'), 'Excluiu')
    await user.selectOptions(screen.getByLabelText('Quem'), 'Bruno Lima (bruno@frota360.teste)')

    await waitFor(() =>
      expect(consultas.at(-1)?.params).toMatchObject({ entidade: 'Despesa', acao: 'Excluiu', usuarioId: '5', pagina: '1' }),
    )
  })

  it('com filtro e nada encontrado, diz isso; "Limpar filtros" desfaz', async () => {
    const { user, consultas } = montar([])
    await screen.findByText(/Nada registrado ainda/)
    await user.selectOptions(screen.getByLabelText('Ação'), 'Calculou rota')
    expect(await screen.findByText('Nenhum registro para os filtros escolhidos.')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Limpar filtros' }))
    await waitFor(() => expect(consultas.at(-1)?.params.acao).toBeUndefined())
  })

  it('"Atualizar" refaz a consulta', async () => {
    const { user, consultas } = montar()
    await linha('Excluiu a despesa')
    await user.click(screen.getByRole('button', { name: 'Atualizar' }))
    await waitFor(() => expect(consultas).toHaveLength(2))
  })

  it('virar de página fecha a linha expandida', async () => {
    const { user } = montar([COM_DIFF, EXCLUSAO], 40)
    await user.click(await screen.findByRole('button', { name: /Atualizou o veículo ABC1D01/ }))
    expect(screen.getByText('Origem: 10.0.0.8')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Próxima' }))
    await waitFor(() => expect(screen.queryByText('Origem: 10.0.0.8')).not.toBeInTheDocument())
  })
})
